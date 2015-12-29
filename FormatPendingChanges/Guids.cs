// Guids.cs
// MUST match guids.h
using System;

namespace Microsoft.FormatPendingChanges
{
    static class GuidList
    {
        public const string guidFormatPendingChangesPkgString = "710d40a6-6456-47bd-b748-dab1d14c1a53";
        public const string guidFormatPendingChangesCmdSetString = "d33683f6-bb8c-4c7f-b806-c3a03dba8d2b";

        public static readonly Guid guidFormatPendingChangesCmdSet = new Guid(guidFormatPendingChangesCmdSetString);
    };
}