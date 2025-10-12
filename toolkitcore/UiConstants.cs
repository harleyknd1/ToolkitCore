// MIT License
// 
// Copyright (c) 2025 sirrandoo
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
using JetBrains.Annotations;
using Verse;

namespace ToolkitCore;

/// <summary>A set of constants that are used throughout the mod's menus.</summary>
[PublicAPI]
public static class UiConstants
{
    /// <summary>The line height of all content within the mod's menus.</summary>
    /// <remarks>
    ///     The mod intentionally uses a slightly higher line height than is typical to create less visually dense menus,
    ///     making it easier to read and navigate through information-heavy screens.
    /// </remarks>
    public const float LineHeight = 28f;

    /// <summary>The height of all tabs within the mod's menus.</summary>
    /// <remarks>
    ///     This constant defines the vertical space allocated for each tab to ensure a consistent and visually appealing
    ///     layout across different menu screens.
    /// </remarks>
    public const float TabHeight = 35f;

    /// <summary>The halved value of the standard small font height used in the mod's menus.</summary>
    /// <remarks>
    ///     This constant is used to maintain consistent spacing and layout for elements that require a smaller line
    ///     height, ensuring readability and visual coherence in the mod's interface.
    /// </remarks>
    public const float HalvedSmallLineHeight = Text.SmallFontHeight * 0.5f;
}