# Changelog

## 2.0.0

* Added .NET 5.0 target.

* `JsonContent` (.NET Standard 2.0 & 2.1 targets) has been updated so that its
  API & namespace (moved from `System.Net.Http` into `System.Net.Http.Json`)
  match what is provided in .NET 5.0. This is a breaking change from 1.x but
  makes it easier to multi-target/migrate code to .NET 5.0.