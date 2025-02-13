﻿// Copyright (c) Josef Pihrt and Contributors. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Roslynator
{
    internal static class GeneratedCodeUtility
    {
        private static readonly char[] _separators = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, Path.VolumeSeparatorChar };

        public static bool IsGeneratedCode(SyntaxTree tree, Func<SyntaxTrivia, bool> isComment, CancellationToken cancellationToken = default)
        {
            return IsGeneratedCodeFile(tree.FilePath)
                || BeginsWithAutoGeneratedComment(tree, isComment, cancellationToken);
        }

        public static bool IsGeneratedCode(
            ISymbol symbol,
            INamedTypeSymbol generatedCodeAttribute,
            Func<SyntaxTrivia, bool> isComment,
            CancellationToken cancellationToken = default)
        {
            if (IsMarkedWithGeneratedCodeAttribute(symbol, generatedCodeAttribute))
                return true;

            if (symbol.Kind != SymbolKind.Namespace)
            {
                foreach (SyntaxReference syntaxReference in symbol.DeclaringSyntaxReferences)
                {
                    SyntaxNode node = syntaxReference.GetSyntax(cancellationToken);

                    SyntaxTree tree = node.SyntaxTree;

                    if (tree != null
                        && IsGeneratedCode(tree, isComment, cancellationToken))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsGeneratedCodeFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            int directorySeparatorIndex = filePath.LastIndexOfAny(_separators);

            if (string.Compare("TemporaryGeneratedFile_", 0, filePath, directorySeparatorIndex + 1, "TemporaryGeneratedFile_".Length, StringComparison.OrdinalIgnoreCase) == 0)
                return true;

            int dotIndex = filePath.LastIndexOf(".", filePath.Length - 1, filePath.Length - directorySeparatorIndex - 1);

            if (dotIndex == -1)
                return false;

            return IsMatch(".Designer")
                || IsMatch(".Generated")
                || IsMatch(".g")
                || IsMatch(".g.i")
                || IsMatch(".AssemblyAttributes");

            bool IsMatch(string value)
            {
                int length = value.Length;

                int index = dotIndex - length;

                return index >= 0
                    && string.Compare(value, 0, filePath, index, length, StringComparison.OrdinalIgnoreCase) == 0;
            }
        }

        internal static bool BeginsWithAutoGeneratedComment(SyntaxTree tree, Func<SyntaxTrivia, bool> isComment, CancellationToken cancellationToken = default)
        {
            return BeginsWithAutoGeneratedComment(tree.GetRoot(cancellationToken), isComment);
        }

        internal static bool BeginsWithAutoGeneratedComment(SyntaxNode root, Func<SyntaxTrivia, bool> isComment)
        {
            foreach (SyntaxTrivia trivia in root.GetLeadingTrivia())
            {
                if (isComment(trivia))
                {
                    string text = trivia.ToString();

                    if (text.Contains("<autogenerated"))
                        return true;

                    if (text.Contains("<auto-generated"))
                        return true;
                }
            }

            return false;
        }

        public static bool IsMarkedWithGeneratedCodeAttribute(
            ISymbol symbol,
            INamedTypeSymbol generatedCodeAttribute)
        {
            if (symbol.Kind != SymbolKind.Namespace
                && symbol.HasAttribute(generatedCodeAttribute))
            {
                return true;
            }

            ISymbol containingSymbol = symbol.ContainingSymbol;

            return containingSymbol != null
                && IsMarkedWithGeneratedCodeAttribute(containingSymbol, generatedCodeAttribute);
        }
    }
}
