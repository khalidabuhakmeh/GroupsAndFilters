# Endpoint Filters in ASP.NET Core 7

This example shows the use of Endpoint filters to provide
cross-cutting functionality on endpoints. 

Given a response of `ILinks`, the filter will traverse the response graph and generate links for each instance of `ILinks` given the metadata found on
the response.

## Getting Started

You'll .NET 7 (preview at the time of this example)