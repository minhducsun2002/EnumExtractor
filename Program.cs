using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Mono.Cecil;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EnumExtractor
{
    class Program
    {
        static void Main(string[] args)
        {
            var loadedModule = ModuleDefinition.ReadModule(args[0]);
            var @namespace = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName("FgoExportedConstants"));

            foreach (var type in loadedModule.Types.OrderBy(type => type.Name))
                if (
                    type.IsClass && !type.FullName.Contains('.')
                    && (type.IsAbstract && type.IsSealed || type.Name.Contains("Entity"))
                )
                {
                    var enums = type.NestedTypes.Where(field => field.IsEnum).ToArray();
                    if (enums.Length == 0) continue;

                    var @class = SyntaxFactory.ClassDeclaration(type.Name);
                    if (type.IsAbstract && type.IsSealed) // static keyword
                        @class = @class.AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword)); // class declaration
                    foreach (var @enum in enums)
                    {
                        var enumDeclaration = SyntaxFactory.EnumDeclaration(@enum.Name); // enum declaration
                        bool hasLong = false;
                        foreach (var enumValue in @enum.Fields.Where(e => e.Name != "value__"))
                        {
                            var name = enumValue.Name;
                            var value = enumValue.Constant;
                            SyntaxToken valueToken;
                            try
                            {
                                valueToken = SyntaxFactory.Literal((int) value);
                            }
                            catch
                            {
                                valueToken = SyntaxFactory.Literal((long) value);
                                hasLong = true;
                            }
                            
                            var valueExpression = SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                valueToken
                            ));
                            var enumMember = SyntaxFactory.EnumMemberDeclaration(
                                    new SyntaxList<AttributeListSyntax>(),
                                    SyntaxFactory.Identifier(name),
                                    valueExpression
                            );
                            enumDeclaration = enumDeclaration.AddMembers(enumMember);   // adding enum values
                        }

                        if (hasLong)
                            enumDeclaration = enumDeclaration.AddBaseListTypes(
                                SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("long"))
                            );
                        
                        // adding enum field to class
                        @class = @class.AddMembers(enumDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword)));
                    }
                    
                    @namespace = @namespace.AddMembers(@class.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))); // adding class member to namespace
                }
            
            Console.WriteLine(@namespace.NormalizeWhitespace().ToFullString());
        }
    }
}
