# VesselQuery

An in-memory database and a small SQL-like query language, written from scratch in C# / .NET 10.
It loads 50,000 vessel records into memory and runs queries like this against them:

```sql
WHERE Z13_STATUS_CODE = 4 AND BUILDER_GROUP = 'Guoyu Logistics'
```

There is no database library. The tokenizer, parser and evaluator are all hand-written.

---

## Quick start

Requirements: the [.NET 10 SDK](https://dotnet.microsoft.com/download). The dataset is included in the repo, so nothing else needs to be downloaded.

```bash
dotnet run --project VesselQuery.Api --launch-profile http
```

- Swagger UI opens at **http://localhost:5285/swagger**. Expand `POST /api/vessels/query`, click *Try it out*, and send:
  ```json
  { "query": "WHERE Z13_STATUS_CODE = 4 AND BUILDER_GROUP = 'Guoyu Logistics'" }
  ```
  The response has `"total": 18`, which are the same 18 vessels as in `Query1.json`.
- To run the same queries without Swagger, open [VesselQuery.Api/VesselQuery.Api.http](VesselQuery.Api/VesselQuery.Api.http) in VS Code (REST Client) or Visual Studio. It has ready-made requests.
- Or use curl:
  ```bash
  curl -X POST http://localhost:5285/api/vessels/query \
       -H "Content-Type: application/json" \
       -d "{\"query\": \"WHERE Z13_STATUS_CODE < 4\", \"take\": 5}"
  ```

Run the tests (104 tests, including the 18-row check against the real data):

```bash
dotnet test
```

### API

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/vessels/query` | Body: `{ "query": "...", "skip": 0, "take": 100 }`. Returns `{ query, total, skip, take, results }`. `take` is capped at 1000. |
| `GET` | `/api/vessels/fields` | Lists the 63 field names you can query. |

A bad query returns **400** with a [ProblemDetails](https://www.rfc-editor.org/rfc/rfc9457) body that says what went wrong and where:

```json
{ "title": "Invalid query", "status": 400, "detail": "Unknown field 'BUILDR_GROUP'" }
```

---

## The query language

| Feature | Example | Status |
|---|---|---|
| Numeric `<` `>` `=` | `WHERE Z13_STATUS_CODE < 4` | required by the brief |
| Text `=` | `WHERE BUILDER_GROUP = 'Guoyu Logistics'` | required by the brief |
| `AND` | `WHERE Z13_STATUS_CODE = 4 AND BUILDER_GROUP = 'Guoyu Logistics'` | optional part of the brief |
| `OR`, `NOT`, `( )` | `WHERE (FLAG = 'Panama' OR FLAG = 'Liberia') AND NOT A12_YEAR_BUILT < 2000` | extension |
| `<=` `>=` `!=` `<>` | `WHERE A04_DWT_tonnes >= 100000` | extension |

Details:
- `WHERE` is optional. Keywords and field names are case-insensitive.
- Strings can use `'…'`, `"…"` or curly quotes `‘…’` / `“…”`. The brief itself uses curly quotes, and text copied from Word or email usually has them. To put a quote inside a string, double it: `'O''Brien'`.
- Numbers can be negative or decimal: `-4`, `42.8`, `.5`.

### Semantics

| Rule | Why |
|---|---|
| Text `=` is **case-insensitive** | `'guoyu logistics'` should find `Guoyu Logistics`. This matches SQL Server's default collation. |
| A **null** field never matches, not even with `!=` | This is SQL's NULL rule, simplified. `L91_HULL_TYPE != 'x'` does not match rows where the hull type is unknown. |
| A **type mismatch** does not match | `Z13_STATUS_CODE = '4'` compares a number to text, so it is false. It is not converted. |
| `<` `>` on text is an **error** | It is rejected at parse time, not left to return something surprising. |
| An **unknown field** is an **error** | Without this, a typo like `BUILDR_GROUP` would return 0 rows and look like a real "no results". |

---

## How it works

```
 HTTP  POST /api/vessels/query  { "query": "WHERE ..." }
   │
   ▼
 VesselsController           (Api)   HTTP only: binds and validates the request, returns JSON
   │
   ▼
 VesselQueryService          (Core)  runs the query:
   │                                   1. parse       string ─► tree
   │                                   2. validate    every field in the tree exists
   │                                   3. scan        test every row, count matches, keep one page
   ├──► Tokenizer ─► QueryParser  ─► QueryNode tree (AST)
   ├──► QueryEvaluator           evaluates the tree against one row ─► true / false
   └──► IVesselStore             the in-memory table (a singleton, loaded once at startup)

 QueryException anywhere ─► QueryExceptionHandler ─► 400 ProblemDetails
```

### 1. Loading the data: `VesselDataLoader` + `InMemoryVesselStore`

- The supplied `vessels.json` is **JavaScript, not JSON**. It is wrapped as `var vessels = [ ... ];`. The loader keeps only the text from the first `[` to the last `]`, then parses that with `System.Text.Json`.
- Each row becomes a `Dictionary<string, object?>`, not a `Vessel` class with 63 properties. That way any field can be queried with no schema code, and a new field in the data needs no code change.
- Each value is converted **once** at load time to `double`, `string`, `bool` or `null`, so the evaluator never touches JSON types.
- The store is registered as a **singleton** and loaded at startup (about 1.5 s). After that, each query is a scan over data already in memory, which takes a few milliseconds.

### 2. Tokenizing: `Tokenizer`

The tokenizer turns the query string into a list of tokens:

```
WHERE Z13_STATUS_CODE = 4 AND BUILDER_GROUP = ‘Guoyu Logistics’
  ↓
[WHERE] [IDENT Z13_STATUS_CODE] [OP =] [NUM 4] [AND] [IDENT BUILDER_GROUP] [OP =] [STR Guoyu Logistics] [END]
```

It reads one character at a time. Letters start a word, which is either a keyword or a field name. Digits start a number. Quotes start a string. `< > = !` start an operator. It checks for two-character operators first, so `<=` is one token, not two. The curly-quote handling is in this one class and nowhere else.

### 3. Parsing: `QueryParser` (recursive descent)

The parser turns the tokens into a tree. The grammar has one method per rule:

```
query      → [WHERE] expression END
expression → term   { OR term }       ← lowest precedence
term       → factor { AND factor }
factor     → NOT factor | "(" expression ")" | comparison
comparison → FIELD operator literal
literal    → NUMBER | STRING
```

The sample query becomes:

```
            AndNode
           /       \
  Comparison        Comparison
  Z13_STATUS_CODE   BUILDER_GROUP
  = 4               = "Guoyu Logistics"
```

**Precedence comes from how the grammar is nested; there is no precedence table.** `expression` handles OR and calls `term`. `term` handles AND and calls `factor`. So AND binds tighter than OR: `A OR B AND C` is read as `A OR (B AND C)`, as in SQL. Parentheses go back up to `expression`, which is why they override precedence.

### 4. Evaluating: `QueryEvaluator`

Evaluation is one recursive `switch` over the tree, run once per row:

```csharp
AndNode and   => Evaluate(and.Left, row) && Evaluate(and.Right, row),
OrNode or     => Evaluate(or.Left, row)  || Evaluate(or.Right, row),
NotNode not   => !Evaluate(not.Operand, row),
ComparisonNode c => Compare(c, row),
```

`&&` and `||` short-circuit, so once `Z13_STATUS_CODE = 4` fails, the text comparison for that row is skipped.

### Project layout

```
VesselQuery.Core/        the engine: no ASP.NET dependency, so it can be reused (console app, tests, ...)
  Querying/              Tokenizer, QueryParser, AST records, QueryEvaluator, QueryException
  Data/                  VesselDataLoader, IVesselStore, InMemoryVesselStore
  Services/              VesselQueryService (parse → validate → scan → page)
VesselQuery.Api/         thin HTTP layer
  Controllers/           VesselsController
  Contracts/             request and response DTOs, with validation attributes
  Configuration/         options binding and DI registration (AddVesselQuery)
  Infrastructure/        QueryExceptionHandler (QueryException → 400)
  Data/vessels.json.gz   the dataset
VesselQuery.Tests/       xUnit: unit tests for each layer and integration tests on the real data and the HTTP API
  TestData/Query1.json   the expected result supplied with the brief
```

### Why is the dataset gzipped?

`vessels.json` is **117 MB**, and GitHub rejects any file over **100 MB**, so the raw file can't be committed. Gzipped it is **9.9 MB**. The file is compressed unchanged, and the loader decompresses it as it reads. This keeps the repo self-contained: clone it, run it, get results. The path is set in `appsettings.json` (`VesselData:FilePath`) and can point at an uncompressed `.json` file too. The other options were Git LFS, which needs extra setup for everyone who clones, or a download step, which breaks "clone and run".

---

## Extending the language

The design makes each extension a small change to one layer:

| Feature | Tokenizer | Parser | Evaluator |
|---|---|---|---|
| `IN ('Panama', 'Liberia')` | `IN` keyword, `,` token | `comparison → FIELD IN "(" literal {"," literal} ")"` → `InNode` | membership test in a `HashSet` |
| `LIKE 'Guoyu%'` | `LIKE` keyword | new operator | turn the pattern into a prefix, suffix or contains check (or a regex) |
| `IS [NOT] NULL` | `IS`, `NULL` keywords | new `IsNullNode` | `value is null` (the only way to match nulls, as in SQL) |
| `BETWEEN 1 AND 5` | `BETWEEN` keyword | parse into `AndNode(>= 1, <= 5)`, so no evaluator change is needed | none |
| Dates (`Z05_DATE_BUILT > '2010-01-01'`) | none | none | compare as `DateTime` when both sides parse as dates |
| `SELECT fields`, `ORDER BY`, `LIMIT` | keywords | new top-level `query` rule | projection and sorting after filtering |

Performance, when the data or query volume grows:

1. **Compile the tree to a delegate.** Turn the AST into a `Func<Row, bool>` once, using `System.Linq.Expressions` or closures, so each row doesn't walk the tree again.
2. **Indexes.** Build a hash index (`Dictionary<value, List<row>>`) for `=` on common fields, and a sorted index for ranges (`<`, `>`). The sample query would then look up `BUILDER_GROUP = 'Guoyu Logistics'` directly (a few dozen rows) instead of scanning 50,000.
3. **A simple query planner.** For an `AND`, evaluate the most selective side first, using an index when one exists. The short-circuit in the evaluator already gives part of this.
4. **Columnar storage.** Store each field as its own array instead of a dictionary per row. This uses less memory and makes scans faster.
