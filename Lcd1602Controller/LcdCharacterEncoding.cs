﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Lcd1602Controller
{
    /// <summary>
    /// Provides the character encoding for an LCD display.
    /// Instances of this class are generated by <see cref="LcdCharacterEncodingFactory"/>.
    /// </summary>
    public class LcdCharacterEncoding : Encoding
    {
        private Dictionary<char, byte> _characterMapping;
        private char _unknownLetter;
        // Character pixel maps for characters that will need to be written to the character RAM for this code page
        private List<byte[]> _extraCharacters;
        private string _cultureName;

        /// <summary>
        /// Creates an instance of <see cref="LcdCharacterEncoding"/>.
        /// </summary>
        /// <param name="cultureName">Culture name for this encoding (informational only)</param>
        /// <param name="readOnlyMemoryName">Name of the ROM (hard coded read-only character memory) on the display</param>
        /// <param name="characterMap">The character map to use</param>
        /// <param name="unknownLetter">The character to print when a letter not in the map is found</param>
        public LcdCharacterEncoding(string cultureName, string readOnlyMemoryName, Dictionary<char, byte> characterMap, char unknownLetter)
        {
            _cultureName = cultureName;
            _characterMapping = characterMap;
            _unknownLetter = unknownLetter;
            _extraCharacters = new List<byte[]>();
            AllCharactersSupported = true;
            ReadOnlyMemoryName = readOnlyMemoryName;
        }

        /// <summary>
        /// Creates an instance of <see cref="LcdCharacterEncoding"/>.
        /// </summary>
        /// <param name="cultureName">Culture name for this encoding (informational only)</param>
        /// <param name="readOnlyMemoryName">Name of the ROM (hard coded read-only character memory) on the display</param>
        /// <param name="characterMap">The character map to use</param>
        /// <param name="unknownLetter">The character to print when a letter not in the map is found</param>
        /// <param name="extraCharacters">The pixel map of characters required for this culture but not found in the character ROM</param>
        public LcdCharacterEncoding(string cultureName, string readOnlyMemoryName, Dictionary<char, byte> characterMap, char unknownLetter, List<byte[]> extraCharacters)
            : this(cultureName, readOnlyMemoryName, characterMap, unknownLetter)
        {
            _extraCharacters = extraCharacters;
            AllCharactersSupported = true;
        }

        /// <summary>
        /// Always returns true for this class.
        /// </summary>
        public override bool IsSingleByte => true;

        /// <inheritDoc/>
        public override string EncodingName => _cultureName;

        /// <summary>
        /// The list of pixel maps for extra characters that are required for this culture.
        /// </summary>
        public virtual List<byte[]> ExtraCharacters
        {
            get
            {
                return _extraCharacters;
            }
        }

        /// <summary>
        /// This is internally set to false if we already know that we won't be able to display all required characters
        /// </summary>
        protected internal bool AllCharactersSupported
        {
            get;
            set;
        }

        /// <summary>
        /// Specified Name of the hardcoded character memory set for which this Encoding is intended. An encoding shall only be loaded to
        /// a matching display.
        /// </summary>
        public string ReadOnlyMemoryName
        {
            get;
        }

        /// <inheritDoc/>
        public override int GetByteCount(char[] chars, int index, int count)
        {
            if (index + count > chars.Length || index < 0 || count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index + count must be smaller than array length");
            }

            return count;
        }

        /// <inheritDoc/>
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            for (int i = 0; i < charCount; i++)
            {
                byte b;
                if (_characterMapping == null)
                {
                    bytes[byteIndex] = (byte)chars[charIndex];
                }
                else if (_characterMapping.TryGetValue(chars[charIndex], out b))
                {
                    bytes[byteIndex] = b;
                }
                else
                {

                    bytes[byteIndex] = _characterMapping[_unknownLetter];
                }

                byteIndex++;
                charIndex++;
            }

            return charCount;
        }

        /// <inheritDoc/>
        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            return Math.Min(bytes.Length, count);
        }

        /// <summary>
        /// Reverse mapping is not supported for this encoding.
        /// </summary>
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            throw new NotSupportedException("Reverse conversion not supported");
        }

        /// <inheritDoc/>
        public override int GetMaxByteCount(int charCount)
        {
            // This encoder always does a 1:1 mapping
            return charCount;
        }

        /// <inheritDoc/>
        public override int GetMaxCharCount(int byteCount)
        {
            return byteCount;
        }
    }
}