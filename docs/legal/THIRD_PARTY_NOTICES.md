# Thyme-Me third-party notices

Thyme-Me includes or is built with third-party software. Each component
remains governed by its own terms. The release SBOM is the authoritative inventory
for the exact artifact; this file records the principal redistributed families.

| Component family | Version in 1.0.3 | License |
| --- | --- | --- |
| .NET runtime and Microsoft.Extensions | 10.0.11 | MIT |
| Entity Framework Core and Microsoft.Data.Sqlite | 10.0.11 | MIT |
| CommunityToolkit.Mvvm | 8.4.2 | MIT |
| SQLitePCLRaw | 2.1.12 | Apache-2.0 |
| SQLite | bundled by SQLitePCLRaw | Public domain |
| Microsoft WebView2 SDK | 1.0.3719.77 | BSD-3-Clause-style Microsoft terms below |
| Microsoft Windows App SDK runtime family | 2.4.0 and its resolved components | Microsoft Windows App SDK license |
| Microsoft Windows ML runtime pulled by Windows App SDK | 2.1.74 | Microsoft Windows ML Runtime license |

The Windows App SDK license permits redistribution of files placed with an
application by the WindowsAppSDK NuGet package, subject to its requirements. The
current terms are distributed in the corresponding NuGet packages and published
by Microsoft at https://aka.ms/WindowsAppSDK. Windows ML terms are distributed in
the corresponding Microsoft.Windows.AI.MachineLearning package.

## MIT License

Copyright holders are identified in the respective package metadata.

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in the
Software without restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the
Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN
AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

## Apache License 2.0

SQLitePCLRaw is Copyright 2014-2024 SourceGear, LLC and licensed under the Apache
License, Version 2.0. The complete license is available at
https://www.apache.org/licenses/LICENSE-2.0 and in the corresponding NuGet
packages. Required notice: you may not use a contributor's name to endorse a
derived product, and modifications must be identified as required by that license.

## Microsoft WebView2 SDK terms

Copyright (C) Microsoft Corporation. All rights reserved.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that source redistributions retain the copyright notice,
conditions, and disclaimer; binary redistributions reproduce them in associated
documentation or materials; and Microsoft or contributor names are not used to
endorse or promote derived products without prior written permission.

THE SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF
THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
