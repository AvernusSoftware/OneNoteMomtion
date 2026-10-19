using NUnit.Framework;

// The default "language": "auto" follows the Windows UI language, so without this every assertion on an
// English message would fail on a Polish machine. Tests of other languages set the language explicitly.
[assembly: SetUICulture("en-US")]
