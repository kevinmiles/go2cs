﻿//******************************************************************************************************
//  ScannerBase_Type.cs - Gbtc
//
//  Copyright © 2018, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may not use this
//  file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  05/03/2018 - J. Ritchie Carroll
//       Generated original version of source code.
//
//******************************************************************************************************

using go2cs.Metadata;
using System.Linq;

namespace go2cs
{
    public partial class ScannerBase
    {
        // Stack handlers:
        //  constSpec (optional)
        //  typeSpec (required)
        //  varSpec (required)
        //  typeList (required)
        //  type (optional)
        //  elementType (required)
        //  pointerType (required)
        //  mapType (required)
        //  result (optional)
        //  parameterDecl (required)
        //  fieldDecl (optional)
        //  typeAssertion (required)
        //  arguments (optional)
        //  conversion (required)
        protected readonly ParseTreeValues<TypeInfo> Types = new ParseTreeValues<TypeInfo>();

        //public override void EnterType_(GoParser.Type_Context context)
        //{
        //    Debug.WriteLine($"Enter type: {context.GetText()} -- {context}");
        //}

        public override void ExitType_(GoParser.Type_Context context)
        {
            TypeInfo typeInfo;

            if (!(context.type_() is null))
            {
                if (Types.TryGetValue(context.type_(), out typeInfo))
                    Types[context] = typeInfo;
                else
                    AddWarning(context, $"Failed to find sub-type in type expression \"{context.GetText()}\"");
            }

            if (context.Parent is GoParser.ElementTypeContext elementType && Types.TryGetValue(context, out typeInfo))
                Types[elementType] = typeInfo;
        }

        public override void EnterTypeName(GoParser.TypeNameContext context)
        {
            string type = context.GetText();
            string typeName = ConvertToCSTypeName(type);
            TypeClass typeClass = TypeClass.Simple;

            if (typeName.Equals("error") || (Metadata?.Interfaces.TryGetValue(typeName, out _) ?? false))
                typeClass = TypeClass.Interface;
            else if (Metadata?.Structs.TryGetValue(typeName, out _) ?? false)
                typeClass = TypeClass.Struct;

            Types[context.Parent] = new TypeInfo
            {
                Name = type,
                TypeName = ConvertToCSTypeName(type),
                FullTypeName = typeName,
                TypeClass = typeClass
            };
        }

        public override void ExitPointerType(GoParser.PointerTypeContext context)
        {
            string name = context.GetText();

            if (!Types.TryGetValue(context.type_(), out TypeInfo typeInfo))
            {
                AddWarning(context, $"Failed to find pointer type info for \"{name}\"");
                return;
            }

            if (name.StartsWith("**"))
            {
                int count = 0;

                while (name[count] == '*')
                    count++;

                count = count - (typeInfo.IsByRefPointer ? 1 : 0);

                string prefix = string.Join("", Enumerable.Range(0, count).Select(i => "ptr<"));
                string suffix = new string('>', count);

                typeInfo = ConvertByRefToBasicPointer(typeInfo);

                Types[context.Parent.Parent] = new TypeInfo
                {
                    Name = name,
                    TypeName = $"{prefix}{typeInfo.TypeName}{suffix}",
                    FullTypeName = $"{prefix}{typeInfo.FullTypeName}{suffix}",
                    IsPointer = true,
                    IsByRefPointer = false,
                    TypeClass = TypeClass.Simple
                };
            }
            else if (name.StartsWith("*(*") && name.EndsWith(")"))
            {
                typeInfo = ConvertByRefToNativePointer(typeInfo);

                Types[context.Parent.Parent] = new TypeInfo
                {
                    Name = name,
                    TypeName = $"*({typeInfo.TypeName})",
                    FullTypeName = $"*({typeInfo.FullTypeName})",
                    IsPointer = true,
                    IsByRefPointer = false,
                    TypeClass = TypeClass.Simple
                };

                UsesUnsafePointers = true;
            }
            else
            {
                Types[context.Parent.Parent] = new TypeInfo
                {
                    Name = name,
                    TypeName = $"ref {typeInfo.TypeName}",
                    FullTypeName = $"ref {typeInfo.FullTypeName}",
                    IsPointer = true,
                    IsByRefPointer = true,
                    TypeClass = TypeClass.Simple
                };
            }
        }

        public override void ExitArrayType(GoParser.ArrayTypeContext context)
        {
            string name = context.GetText();

            if (!Types.TryGetValue(context.elementType().type_(), out TypeInfo typeInfo))
            {
                AddWarning(context, $"Failed to find array type info for \"{name}\"");
                return;
            }

            ExpressionInfo length;

            if (Expressions.TryGetValue(context.arrayLength().expression(), out ExpressionInfo expression))
            {
                length = new ExpressionInfo
                {
                    Text = expression.Text,
                    Type = new TypeInfo
                    {
                        TypeClass = TypeClass.Simple,
                        TypeName = "long",
                        FullTypeName = "System.Int64",
                        IsConst = true
                    }
                };
            }
            else
            {
                length = new ExpressionInfo
                {
                    Text = "0",
                    Type = new TypeInfo
                    {
                        TypeClass = TypeClass.Simple,
                        TypeName = "long",
                        FullTypeName = "System.Int64",
                        IsConst = true
                    }
                };

                AddWarning(context, $"Failed to find array length expression for \"{name}\"");
            }

            Types[context.Parent.Parent] = new ArrayTypeInfo
            {
                Name = name,
                TypeName = $"array<{typeInfo.TypeName}>",
                FullTypeName = $"go.array<{typeInfo.FullTypeName}>",
                TypeClass = TypeClass.Array,
                Length = length
            };
        }

        public override void ExitMapType(GoParser.MapTypeContext context)
        {
            string type = context.GetText();

            Types.TryGetValue(context.type_(), out TypeInfo keyTypeInfo);

            if (keyTypeInfo is null)
                return; // throw new InvalidOperationException("Map key type undefined.");

            Types.TryGetValue(context.elementType().type_(), out TypeInfo elementTypeInfo);

            if (elementTypeInfo is null)
                return; // throw new InvalidOperationException("Map element type undefined.");

            Types[context.Parent.Parent] = new MapTypeInfo
            {
                Name = type,
                TypeName = $"map<{keyTypeInfo.TypeName}, {elementTypeInfo.TypeName}>",
                FullTypeName = $"go.map<{keyTypeInfo.FullTypeName}, {elementTypeInfo.FullTypeName}>",
                ElementTypeInfo = elementTypeInfo,
                KeyTypeInfo = keyTypeInfo,
                TypeClass = TypeClass.Map
            };
        }

        public override void ExitSliceType(GoParser.SliceTypeContext context)
        {
            Types.TryGetValue(context.elementType().type_(), out TypeInfo typeInfo);

            if (typeInfo is null)
                typeInfo = TypeInfo.ObjectType;

            Types[context.Parent.Parent] = new TypeInfo
            {
                Name = typeInfo.Name,
                TypeName = $"slice<{typeInfo.TypeName}>",
                FullTypeName = $"go.slice<{typeInfo.FullTypeName}>",
                TypeClass = TypeClass.Slice
            };
        }

        public override void ExitChannelType(GoParser.ChannelTypeContext context)
        {
            // TODO: Update to reference proper channel type name when added
            Types.TryGetValue(context.elementType().type_(), out TypeInfo typeInfo);

            if (typeInfo is null)
                typeInfo = TypeInfo.ObjectType;

            Types[context.Parent.Parent] = new TypeInfo
            {
                Name = typeInfo.Name,
                TypeName = $"channel<{typeInfo.TypeName}>",
                FullTypeName = $"go.channel<{typeInfo.FullTypeName}>",
                TypeClass = TypeClass.Channel
            };
        }

        public override void ExitInterfaceType(GoParser.InterfaceTypeContext context)
        {
            if (context.methodSpec()?.Length == 0)
            {
                // Handle empty interface type as a special case
                Types[context.Parent.Parent] = TypeInfo.ObjectType;

                // Object is more universal than EmptyInterface - which is handy when literals are involved
                //Types[context.Parent.Parent] = TypeInfo.EmptyInterfaceType;
            }
            else
            {
                // TODO: Turn into a strongly typed object and declare prior to function
                // All other intra-function scoped declared interfaces
                // are defined as dynamic so they can behave like ducks
                Types[context.Parent.Parent] = TypeInfo.DynamicType;
            }
        }

        public override void ExitStructType(GoParser.StructTypeContext context)
        {
            // TODO: Turn into a strongly typed object and declare prior to function
            // All intra-function scoped declared structures are
            // defined as dynamic so they can behave like ducks
            Types[context.Parent.Parent] = TypeInfo.DynamicType;
        }

        public override void ExitFunctionType(GoParser.FunctionTypeContext context)
        {
            Signatures.TryGetValue(context.signature(), out Signature signature);

            string typeList = signature.GenerateParameterTypeList();
            string resultSignature = signature.GenerateResultSignature();
            string typeName, fullTypeName;

            RequiredUsings.Add("System");

            if (resultSignature == "void")
            {
                if (string.IsNullOrEmpty(typeList))
                {
                    typeName = "Action";
                    fullTypeName = "System.Action";
                }
                else
                {
                    typeName = $"Action<{typeList}>";
                    fullTypeName = $"System.Action<{typeList}>";
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(typeList))
                    typeList = $"{typeList}, ";

                typeName = $"Func<{typeList}{resultSignature}>";
                fullTypeName = $"System.Func<{typeList}{resultSignature}>";
            }

            Types[context.Parent.Parent] = new TypeInfo
            {
                Name = context.GetText(),
                TypeName = typeName,
                FullTypeName = fullTypeName,
                TypeClass = TypeClass.Function
            };
        }

        protected string ConvertToCSTypeName(string type)
        {
            string primitiveType = ConvertToFullCSTypeName(type);

            if (primitiveType.StartsWith("go."))
                return primitiveType.Substring(3);

            return primitiveType;
        }

        protected string ConvertToFullCSTypeName(string type)
        {
            switch (type)
            {
                case "bool":
                    return "bool";
                case "int8":
                    return "sbyte";
                case "uint8":
                case "byte":
                    return "byte";
                case "int16":
                    return "short";
                case "uint16":
                    return "ushort";
                case "int32":
                case "rune":
                    return "int";
                case "uint32":
                    return "uint";
                case "int64":
                case "int":
                    return "long";
                case "uint64":
                case "uint":
                    return "ulong";
                case "float32":
                    return "float";
                case "float64":
                    return "double";
                case "uintptr":
                    return "System.UIntPtr";
                case "complex64":
                    return "go.complex64";
                case "complex128":
                    return "System.Numerics.Complex128";
                case "string":
                    return "go.@string";
                default:
                    return $"{type}";
            }
        }
    }
}
