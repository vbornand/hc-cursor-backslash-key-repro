# Cursor paging breaks on string sort keys ending with a backslash

Minimal reproduction for GreenDonut.Data 16.7.4 (same code on `main`).

Cursor keys are joined with `:`, and a `:` inside a key is escaped as `\:`. A backslash is
not escaped, so a key ending with `\` merges with the following separator: the cursor
cannot be read back correctly.

## Run

```shell
dotnet run
```

Requires the .NET 10 SDK. SQLite runs in memory, nothing to set up.

It pages four rows one at a time (`First = 1`, then `After = endCursor`), sorted on a string
column where one value ends with a backslash.

## Output

```text
== Sorted on Path, then Id (expected: C:\data, C:\temp\, D:\logs, E:\backup)
page 1: C:\data
page 2: C:\temp\
page 3: C:\temp\
page 4: C:\temp\

== Sorted on Path only (unique) (expected: C:\data, C:\temp\, D:\logs, E:\backup)
page 1: C:\data
page 2: C:\temp\
System.IndexOutOfRangeException: Index was outside the bounds of the array.
   at GreenDonut.Data.Cursors.CursorKeySerializerHelper.RestoreColons(ReadOnlySpan`1 formattedKey, Int32 start, ICursorKeySerializer serializer)
   at GreenDonut.Data.Cursors.CursorKeySerializerHelper.Parse(ReadOnlySpan`1 formattedKey, ICursorKeySerializer serializer)
   at GreenDonut.Data.Cursors.CursorKey.Parse(ReadOnlySpan`1 cursorValue)
   at GreenDonut.Data.Cursors.CursorParser.Parse(String cursor, ReadOnlySpan`1 keys)
   ...
```

With two keys, the end cursor of page 2 is `{}C\:\temp\:2`, read back as one key
`C:\temp:2` and a `null` id: paging returns the same row forever. With a single key, the
cursor `{}C\:\temp\` ends with a lone backslash and parsing reads past the end.
