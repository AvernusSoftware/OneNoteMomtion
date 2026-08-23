using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OneNoteMomtion.Core.Highlighting.Providers
{
    public sealed class SqlHighlightProvider : RegexTokenizerBase
    {
        private const RegexOptions IgnoreCase = RegexOptions.IgnoreCase;

        private static readonly IReadOnlyList<TokenRule> RuleSet = new[]
        {
            TokenRule.Create(@"\s+", TokenKind.Plain),

            TokenRule.Create(@"/\*[\s\S]*?\*/", TokenKind.Comment),
            TokenRule.Create(@"--[^\r\n]*", TokenKind.Comment),
            TokenRule.Create(@"N?'(?:''|[^'])*'", TokenKind.String),
            TokenRule.Create(@"""[^""\r\n]*""|\[[^\]\r\n]*\]|`[^`\r\n]*`", TokenKind.Identifier),
            TokenRule.Create(@"\b\d+(?:\.\d+)?(?:[eE][+-]?\d+)?\b", TokenKind.Number),
            TokenRule.Create(@"@@?[A-Za-z_][A-Za-z0-9_]*|:[A-Za-z_][A-Za-z0-9_]*", TokenKind.Preprocessor),

            TokenRule.Words(
                TokenKind.ControlKeyword,
                IgnoreCase,
                "case", "when", "then", "else", "end", "if", "while", "loop", "begin", "return",
                "break", "continue", "goto", "raise", "throw"),

            TokenRule.Words(
                TokenKind.Keyword,
                IgnoreCase,
                "add", "all", "alter", "and", "any", "as", "asc", "between", "by", "cascade", "check",
                "column", "commit", "constraint", "create", "cross", "database", "declare", "default",
                "delete", "desc", "distinct", "drop", "except", "exec", "execute", "exists", "foreign",
                "from", "full", "grant", "group", "having", "in", "index", "inner", "insert", "intersect",
                "into", "is", "join", "key", "left", "like", "limit", "merge", "not", "null", "offset",
                "on", "or", "order", "outer", "over", "partition", "primary", "procedure", "references",
                "returning", "revoke", "right", "rollback", "row", "rows", "schema", "select", "set",
                "table", "top", "transaction", "trigger", "truncate", "union", "unique", "update", "using",
                "values", "view", "where", "with"),

            TokenRule.Words(
                TokenKind.Type,
                IgnoreCase,
                "bigint", "binary", "bit", "boolean", "char", "date", "datetime", "datetime2", "decimal",
                "float", "int", "integer", "money", "nchar", "numeric", "nvarchar", "real", "smallint",
                "text", "time", "timestamp", "tinyint", "uniqueidentifier", "uuid", "varbinary", "varchar", "xml"),

            TokenRule.Words(
                TokenKind.Identifier,
                IgnoreCase,
                "abs", "avg", "cast", "ceiling", "coalesce", "concat", "convert", "count", "dateadd",
                "datediff", "floor", "getdate", "isnull", "len", "lower", "ltrim", "max", "min", "nullif",
                "rank", "replace", "round", "row_number", "rtrim", "substring", "sum", "trim", "upper"),

            TokenRule.Create(@"[A-Za-z_#][A-Za-z0-9_#$]*", TokenKind.Identifier),
            TokenRule.Create(@"<>|!=|>=|<=|\|\||[-+*/%=<>]", TokenKind.Operator),
            TokenRule.Create(@"[(),;.]", TokenKind.Punctuation),
        };

        public override string LanguageId => "sql";

        public override string DisplayName => "SQL";

        protected override IReadOnlyList<TokenRule> Rules => RuleSet;
    }
}
