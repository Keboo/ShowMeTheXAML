
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Xml.Linq;
using Confuser.Renamer.BAML;

// ReSharper disable once CheckNamespace
public class ModuleWeaver
{
    // Will contain the full element XML from FodyWeavers.xml. OPTIONAL
    public XElement Config { get; set; }

    // Will log an MessageImportance.Normal message to MSBuild. OPTIONAL
    public Action<string> LogDebug { get; set; } = s => { };

    // Will log an MessageImportance.High message to MSBuild. OPTIONAL
    public Action<string> LogInfo { get; set; } = s => { };

    // Will log an warning message to MSBuild. OPTIONAL
    public Action<string> LogWarning { get; set; } = s => { };

    // Will log an warning message to MSBuild at a specific point in the code. OPTIONAL
    public Action<string, SequencePoint> LogWarningPoint { get; set; } = (s, p) => { };

    // Will log an error message to MSBuild. OPTIONAL
    public Action<string> LogError { get; set; } = s => { };

    // Will log an error message to MSBuild at a specific point in the code. OPTIONAL
    public Action<string, SequencePoint> LogErrorPoint { get; set; } = (s, p) => { };

    // An instance of Mono.Cecil.IAssemblyResolver for resolving assembly references. OPTIONAL
    public IAssemblyResolver AssemblyResolver { get; set; }

    // An instance of Mono.Cecil.ModuleDefinition for processing. REQUIRED
    public ModuleDefinition ModuleDefinition { get; set; }

    // Will contain the full path of the target assembly. OPTIONAL
    public string AssemblyFilePath { get; set; }

    // Will contain the full directory path of the target project. 
    // A copy of $(ProjectDir). OPTIONAL
    public string ProjectDirectoryPath { get; set; }

    // Will contain the full directory path of the current weaver. OPTIONAL
    public string AddinDirectoryPath { get; set; }

    // Will contain the full directory path of the current solution.
    // A copy of `$(SolutionDir)` or, if it does not exist, a copy of `$(MSBuildProjectDirectory)..\..\..\`. OPTIONAL
    public string SolutionDirectoryPath { get; set; }

    // Will contain a semicomma delimetered string that contains 
    // all the references for the target project. 
    // A copy of the contents of the @(ReferencePath). OPTIONAL
    public string References { get; set; }

    // Will a list of all the references marked as copy-local. 
    // A copy of the contents of the @(ReferenceCopyLocalPaths). OPTIONAL
    public List<string> ReferenceCopyLocalPaths { get; set; }

    // Will a list of all the msbuild constants. 
    // A copy of the contents of the $(DefineConstants). OPTIONAL
    public List<string> DefineConstants { get; set; }

    public string AssemblyToProcess { get; set; }

    public void Execute()
    {
        foreach (var resource in ModuleDefinition.Resources.OfType<EmbeddedResource>().ToList())
        {
            Stream stream = resource.GetResourceStream();
            if (stream != null)
            {
                using (var ms = new MemoryStream())
                using (ResourceWriter writer = new ResourceWriter(ms))
                using (ResourceReader rr = new ResourceReader(stream))
                {
                    bool replace = false;

                    foreach (DictionaryEntry entry in rr)
                    {
                        if (entry.Key.ToString().EndsWith("baml", StringComparison.OrdinalIgnoreCase) &&
                            entry.Value is Stream rrStream)
                        {
                            replace = true;
                            BamlDocument document;

                            try
                            {
                                document = BamlReader.ReadDocument(rrStream);
                            }
                            catch (Exception ex)
                            {
                                LogWarning($"Error reading {entry.Key}\r\n{ex}");
                                continue;
                            }

                            Stack<(int, ushort?, ushort)> elementStack = new Stack<(int, ushort?, ushort)>();
                            ushort? displayerTypeId = null, displayerAssemblyId = null, keyAttributeId = null;
                            int displayerIndex = 1;
                            List<Func<ushort, ushort>> attributeConversions = new List<Func<ushort, ushort>>();
                            ushort AttributeIdConverter(ushort x)
                            {
                                foreach (var converstion in attributeConversions)
                                {
                                    x = converstion(x);
                                }
                                return x;
                            }

                            bool hasKey = false;
                            ushort typeId = 0;
                            ushort? lastAttributeId = null;
                            Dictionary<ushort, AttributeInfoRecord> seenAttributes =
                                new Dictionary<ushort, AttributeInfoRecord>();

                            for (int i = 0; i < document.Count; i++)
                            {
                                BamlRecord item = document[i];
                                switch (item.Type)
                                {
                                    case BamlRecordType.DocumentStart:

                                        break;
                                    case BamlRecordType.DocumentEnd:

                                        break;
                                    case BamlRecordType.ElementStart:
                                        {
                                            var startRecord = (ElementStartRecord)item;
                                            if (startRecord.TypeId == displayerTypeId || elementStack.Count > 0)
                                            {
                                                elementStack.Push((i, lastAttributeId, typeId));
                                            }
                                        }
                                        break;
                                    case BamlRecordType.ElementEnd:
                                        {
                                            if (elementStack.Count > 0)
                                            {
                                                (int, ushort?, ushort) elementStart = elementStack.Pop();
                                                if (elementStack.Count == 0)
                                                {
                                                    if (!hasKey && displayerTypeId != null)
                                                    {
                                                        bool resetIndex = false;
                                                        int index;
                                                        for (index = elementStart.Item1; document[index + 1] is LineNumberAndPositionRecord; index++) { }
                                                        if (keyAttributeId == null)
                                                        {
                                                            resetIndex = true;
                                                            ushort attributeId = elementStart.Item2 != null
                                                                ? (ushort)(elementStart.Item2 + 1)
                                                                : (ushort)0;
                                                            keyAttributeId = attributeId;
                                                            var keyAttribute = new AttributeInfoRecord
                                                            {
                                                                Name = "Key",
                                                                AttributeId = attributeId,
                                                                OwnerTypeId = displayerTypeId.Value,
                                                                AttributeUsage = 0,
                                                            };
                                                            document.Insert(++index, keyAttribute);
                                                            attributeConversions.Add(x =>
                                                            {
                                                                if (seenAttributes.ContainsKey(x) &&
                                                                    x >= attributeId)
                                                                {
                                                                    return (ushort)(x + 1);
                                                                }
                                                                return x;
                                                            });
                                                        }
                                                        var propertyValue = new PropertyWithConverterRecord
                                                        {
                                                            AttributeId = keyAttributeId.Value,
                                                            Value = $"{entry.Key.ToString().ToLowerInvariant()}_{displayerIndex++}",
                                                            ConverterTypeId = 64921
                                                        };
                                                        document.Insert(++index, propertyValue);
                                                        if (resetIndex)
                                                        {
                                                            typeId = elementStart.Item3;
                                                            i = index;
                                                        }
                                                    }

                                                    hasKey = false;
                                                }
                                            }
                                        }
                                        break;
                                    case BamlRecordType.Property:
                                        {
                                            var propertyRecord = (PropertyRecord)item;
                                            propertyRecord.AttributeId = AttributeIdConverter(propertyRecord.AttributeId);
                                        }
                                        break;
                                    case BamlRecordType.PropertyCustom:
                                        {
                                            var customProperty = (PropertyCustomRecord)item;
                                            customProperty.AttributeId = AttributeIdConverter(customProperty.AttributeId);
                                        }
                                        break;
                                    case BamlRecordType.PropertyComplexStart:
                                        {
                                            var property = (PropertyComplexStartRecord)item;
                                            property.AttributeId = AttributeIdConverter(property.AttributeId);
                                        }
                                        break;
                                    case BamlRecordType.PropertyComplexEnd:

                                        break;
                                    case BamlRecordType.PropertyArrayStart:
                                        {
                                            var property = (PropertyArrayStartRecord)item;
                                            property.AttributeId = AttributeIdConverter(property.AttributeId);
                                        }
                                        break;
                                    case BamlRecordType.PropertyArrayEnd:

                                        break;
                                    case BamlRecordType.PropertyListStart:
                                        {
                                            var property = (PropertyListStartRecord)item;
                                            property.AttributeId = AttributeIdConverter(property.AttributeId);
                                        }
                                        break;
                                    case BamlRecordType.PropertyListEnd:

                                        break;
                                    case BamlRecordType.PropertyDictionaryStart:
                                        {
                                            var property = (PropertyDictionaryStartRecord)item;
                                            property.AttributeId = AttributeIdConverter(property.AttributeId);
                                        }
                                        break;
                                    case BamlRecordType.PropertyDictionaryEnd:

                                        break;
                                    case BamlRecordType.LiteralContent:

                                        break;
                                    case BamlRecordType.Text:

                                        break;
                                    case BamlRecordType.TextWithConverter:

                                        break;
                                    case BamlRecordType.RoutedEvent:

                                        break;
                                    case BamlRecordType.XmlnsProperty:

                                        break;
                                    case BamlRecordType.DefAttribute:

                                        break;
                                    case BamlRecordType.PIMapping:

                                        break;
                                    case BamlRecordType.AssemblyInfo:
                                        {
                                            var assembly = (AssemblyInfoRecord)item;
                                            var assemblyNameReference = AssemblyNameReference.Parse(assembly.AssemblyFullName);
                                            if (assemblyNameReference.Name == "ShowMeTheXAML")
                                            {
                                                displayerAssemblyId = assembly.AssemblyId;
                                            }
                                        }
                                        break;
                                    case BamlRecordType.TypeInfo:
                                        {
                                            var typeRecord = (TypeInfoRecord)item;
                                            if (typeRecord.TypeFullName == "ShowMeTheXAML.XamlDisplay" && typeRecord.AssemblyId == displayerAssemblyId)
                                            {
                                                displayerTypeId = typeRecord.TypeId;
                                            }
                                            typeRecord.TypeId = typeId++;
                                        }
                                        break;
                                    case BamlRecordType.TypeSerializerInfo:

                                        break;
                                    case BamlRecordType.AttributeInfo:
                                        {
                                            var attribRecord = (AttributeInfoRecord)item;
                                            attribRecord.AttributeId = AttributeIdConverter(attribRecord.AttributeId);
                                            lastAttributeId = attribRecord.AttributeId;
                                            if (!seenAttributes.ContainsKey(attribRecord.AttributeId))
                                            {
                                                seenAttributes.Add(attribRecord.AttributeId, attribRecord);
                                            }
#if DEBUG
                                            else
                                            {
                                                LogWarning(
                                                    $"Found duplicate attribute id {attribRecord.AttributeId}");
                                            }
#endif
                                            if (attribRecord.OwnerTypeId == displayerTypeId && attribRecord.Name == "Key")
                                            {
                                                if (elementStack.Count > 0)
                                                {
                                                    hasKey = true;
                                                }
                                                if (keyAttributeId == null)
                                                {
                                                    keyAttributeId = attribRecord.AttributeId;
                                                }
                                                else if (keyAttributeId != attribRecord.AttributeId && keyAttributeId != null)
                                                {
                                                    string fileName = entry.Key.ToString();
                                                    if (fileName.EndsWith(".baml", StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        fileName = fileName.Substring(0, fileName.Length - 5) + ".xaml";
                                                    }
                                                    LogError($"{fileName} Must either specify all XamlDisplayer.Key properties or specify none of them and let them be automatically generated");
                                                }
                                            }
                                        }
                                        break;
                                    case BamlRecordType.StringInfo:

                                        break;
                                    case BamlRecordType.PropertyStringReference:
                                        {
                                            var property = (PropertyStringReferenceRecord)item;
                                            property.AttributeId = AttributeIdConverter(property.AttributeId);
                                        }
                                        break;
                                    case BamlRecordType.PropertyTypeReference:
                                        {
                                            var property = (PropertyTypeReferenceRecord)item;
                                            property.AttributeId = AttributeIdConverter(property.AttributeId);
                                        }
                                        break;
                                    case BamlRecordType.PropertyWithExtension:
                                        {
                                            var property = (PropertyWithExtensionRecord)item;
                                            property.AttributeId = AttributeIdConverter(property.AttributeId);
                                        }
                                        break;
                                    case BamlRecordType.PropertyWithConverter:
                                        {
                                            var property = (PropertyWithConverterRecord)item;
                                            property.AttributeId = AttributeIdConverter(property.AttributeId);
                                        }
                                        break;
                                    case BamlRecordType.DeferableContentStart:

                                        break;
                                    case BamlRecordType.DefAttributeKeyString:

                                        break;
                                    case BamlRecordType.DefAttributeKeyType:

                                        break;
                                    case BamlRecordType.KeyElementStart:

                                        break;
                                    case BamlRecordType.KeyElementEnd:

                                        break;
                                    case BamlRecordType.ConstructorParametersStart:

                                        break;
                                    case BamlRecordType.ConstructorParametersEnd:

                                        break;
                                    case BamlRecordType.ConstructorParameterType:

                                        break;
                                    case BamlRecordType.ConnectionId:

                                        break;
                                    case BamlRecordType.ContentProperty:
                                        {
                                            var contentProperty = (ContentPropertyRecord)item;
                                            contentProperty.AttributeId = AttributeIdConverter(contentProperty.AttributeId);
                                        }
                                        break;
                                    case BamlRecordType.NamedElementStart:

                                        break;
                                    case BamlRecordType.StaticResourceStart:

                                        break;
                                    case BamlRecordType.StaticResourceEnd:

                                        break;
                                    case BamlRecordType.StaticResourceId:

                                        break;
                                    case BamlRecordType.TextWithId:

                                        break;
                                    case BamlRecordType.PresentationOptionsAttribute:

                                        break;
                                    case BamlRecordType.LineNumberAndPosition:

                                        break;
                                    case BamlRecordType.LinePosition:

                                        break;
                                    case BamlRecordType.OptimizedStaticResource:

                                        break;
                                    case BamlRecordType.PropertyWithStaticResourceId:
                                        {
                                            var property = (PropertyWithStaticResourceIdRecord)item;
                                            property.AttributeId = AttributeIdConverter(property.AttributeId);
                                        }
                                        break;
                                    default:
                                        throw new NotSupportedException();
                                }
                            }
                            var outStream = new MemoryStream();
                            BamlWriter.WriteDocument(document, outStream);
                            outStream.Position = 0;

                            writer.AddResource((string)entry.Key, outStream);
                        }
                        else
                        {
                            writer.AddResource(entry.Key.ToString(), entry.Value);
                        }
                    }
                    writer.Generate();
                    if (replace)
                    {
                        ms.Position = 0;
                        ModuleDefinition.Resources.Remove(resource);
                        ModuleDefinition.Resources.Add(new EmbeddedResource(resource.Name, resource.Attributes,
                            ms.ToArray()));
                    }
                }
            }
        }
    }

    // Will be called when a request to cancel the build occurs. OPTIONAL
    public void Cancel()
    {

    }

    // Will be called after all weaving has occurred and the module has been saved. OPTIONAL
    public void AfterWeaving()
    {

    }
}

namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }
    }

    public struct ValueTuple<T1, T2, T3>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;

        public ValueTuple(T1 item1, T2 item2, T3 item3)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
        }
    }
}

