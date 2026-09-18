namespace VesselQuery.Core.Querying;

public abstract record QueryNode;

public sealed record ComparisonNode(string Field, ComparisonOperator Operator, object Value) : QueryNode;

public sealed record AndNode(QueryNode Left, QueryNode Right) : QueryNode;

public sealed record OrNode(QueryNode Left, QueryNode Right) : QueryNode;

public sealed record NotNode(QueryNode Operand) : QueryNode;
