# Changelog

## 2.0.0

* Improved the performance of `LoggerJsonMessage` serialization:

  * `LogLevel` is now a string. `JsonStringEnumMemberConverter` no longer needs
    to be invoked during serialization.

  * `LoggerJsonMessage.Scope` switched to `List<object>`,
    `LoggerJsonMessage.Data` switched to `Dictionary<string, object>`, and
    `LoggerJsonMessageException.InnerExceptions` switched to
    `List<LoggerJsonMessageException>` so `JsonSerializer` can take advantage of
    the `struct` enumerators exposed on the concrete types. Instances will now
    also be pooled (up to 1024 items per type) to reduce allocations and resize
    copying.

  * `LoggerJsonMessageException.InnerExceptions.StackTrace` switched to `string`
    from `IEnumerabl<string>` so that string manipulation doesn't need to be
    performed during message creation.

  * State & Scope processing will now attempt to perform `for` loops over
    `IReadOnlyList`s before performing `foreach` loops over `IEnumerable`s to
    avoid enumerator allocation.

  * `TypeDescriptor.GetProperties` usage replaced by `Type.GetProperties`. The
    resulting property model for a type is now cached to reduce allocations and
    processing time.

  * `IExternalScopeProvider` is now passed into
    `LoggerJsonMessage.FromLoggerData` so that scopes can be processed directly
    onto the message.

  * Added a `struct` enumerator to `FormattedLogValues` (used by `Write*`
    extensions) to eliminate boxing.

  * Updated `LogValuesFormatter` to the latest version from `dotnet\runtime` to
    reduce allocations on startup.
