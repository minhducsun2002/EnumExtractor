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
                // static classes
                if (type.IsClass && type.IsSealed && type.IsAbstract && !type.FullName.Contains('.'))
                {
                    var enums = type.NestedTypes.Where(field => field.IsEnum).ToArray();
                    if (enums.Length == 0) continue;
                    
                    var @class = SyntaxFactory.ClassDeclaration(type.Name)
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword)); // class declaration
                    foreach (var @enum in enums)
                    {
                        var enumDeclaration = SyntaxFactory.EnumDeclaration(@enum.Name);    // enum declaration
                        foreach (var enumValue in @enum.Fields.Where(e => e.Name != "value__"))
                        {
                            var name = enumValue.Name;
                            var value = enumValue.Constant;
                            var valueExpression = SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                SyntaxFactory.Literal((int) value)
                            ));
                            var enumMember = SyntaxFactory.EnumMemberDeclaration(
                                    new SyntaxList<AttributeListSyntax>(),
                                    SyntaxFactory.Identifier(name),
                                    valueExpression
                            );
                            enumDeclaration = enumDeclaration.AddMembers(enumMember);   // adding enum values
                        }

                        // adding enum field to class
                        @class = @class.AddMembers(enumDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword)));
                    }
                    
                    @namespace = @namespace.AddMembers(@class.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))); // adding class member to namespace
                }
            
            Console.WriteLine(@namespace.NormalizeWhitespace().ToFullString());
        }
    }
}
