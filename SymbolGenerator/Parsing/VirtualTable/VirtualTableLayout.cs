using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.SymbolGenerator.Parsing.VirtualTable
{
    public class VirtualTableSpecifier
    {
        public ASTClass OwningClass { get; set; } = null!;
        public List<ASTMethod> VirtualMethods { get; set; } = new();
    }

    public static class VirtualTableLayout
    {
        public static VirtualTableSpecifier[] Generate(ASTClass[] classes)
        {
            List<VirtualTableSpecifier> vtables = new();
            HashSet<ASTClass> visited = new();
            foreach (var cls in classes)
            {
                GenerateSingle(cls, vtables, visited, new HashSet<ASTClass>());
            }
            return [.. vtables];
        }

        private static void GenerateSingle(ASTClass cls, List<VirtualTableSpecifier> tables, HashSet<ASTClass> visited, HashSet<ASTClass> virtualBasesProcessed)
        {
            if (!visited.Add(cls))
                return;

            // Process base classes first
            foreach (var baseClass in cls.DirectBaseClasses)
            {
                GenerateSingle(baseClass.Class, tables, visited, virtualBasesProcessed);
            }

            // Process virtual bases
            foreach (var baseClass in cls.DirectBaseClasses.Where(b => b.IsVirtualBase))
            {
                if (virtualBasesProcessed.Add(baseClass.Class))
                {
                    VirtualTableSpecifier vBaseTable = new()
                    {
                        OwningClass = baseClass.Class,
                        VirtualMethods = []
                    };

                    var destructor = baseClass.Class.Methods.FirstOrDefault(m => m.IsVirtual && m.IsDestructor);
                    if (destructor is not null)
                    {
                        vBaseTable.VirtualMethods.Add(destructor);
                    }

                    foreach (var method in baseClass.Class.Methods.Where(m => m.IsVirtual && !m.IsDestructor))
                    {
                        vBaseTable.VirtualMethods.Add(method);
                    }
                    tables.Add(vBaseTable);
                }

                // Process primary table
                if (cls.OwnsAtLeastOneVirtualMethod())
                {
                    VirtualTableSpecifier primaryTable = new()
                    {
                        OwningClass = cls,
                        VirtualMethods = []
                    };


                }
            }
        }
    }
}
