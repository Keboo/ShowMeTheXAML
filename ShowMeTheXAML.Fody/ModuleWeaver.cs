
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
public partial class ModuleWeaver
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
            LogWarning($"Found resource {resource.Name} - {stream != null}");
            if (stream != null)
            {
                using (var ms = new MemoryStream())
                using (ResourceWriter writer = new ResourceWriter(ms))
                using (ResourceReader rr = new ResourceReader(stream))
                {
                    bool replace = false;
                    foreach (DictionaryEntry entry in rr)
                    {
                        if (entry.Value is Stream rrStream)
                        {
                            replace = true;
                            var document = BamlReader.ReadDocument(rrStream);

                            Stack<(int, ushort?)> elementStack = new Stack<(int, ushort?)>();
                            ushort? displayerTypeId = null, displayerAssemblyId = null, keyAttributeId = null;
                            int displayerIndex = 1;
                            Func<ushort, ushort> typeIdConverter = x => x;
                            Func<ushort, ushort> attributeIdConverter = x => x;
                            bool hasKey = false;
                            ushort typeId = 0;
                            ushort? lastAttributeId = null;
                            for(int i = 0; i < document.Count; i++)
                            {
                                string extra = "";
                                int itemIndex = i;
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
                                            startRecord.TypeId = typeIdConverter(startRecord.TypeId);

                                            extra = $" TypeId {startRecord.TypeId}";
                                            if (startRecord.TypeId == displayerTypeId || elementStack.Count > 0)
                                            {
                                                elementStack.Push((i, lastAttributeId));
                                                if (startRecord.TypeId == displayerTypeId) LogWarning("~~Found XamlDisplayer~~");
                                            }
                                        }
                                        break;
                                    case BamlRecordType.ElementEnd:
                                        {
                                            if (elementStack.Count > 0)
                                            {
                                                (int, ushort?) elementStart = elementStack.Pop();
                                                if (elementStack.Count == 0)
                                                {
                                                    if (!hasKey && displayerTypeId != null)
                                                    {
                                                        int index = elementStart.Item1;
                                                        if (keyAttributeId == null)
                                                        {
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
                                                            LogWarning($"Adding key attribute with id {attributeId}");
                                                        }
                                                        var propertyValue = new PropertyRecord
                                                        {
                                                            AttributeId = keyAttributeId.Value,
                                                            Value = $"{entry.Key.ToString().ToLowerInvariant()}_{displayerIndex++}"
                                                        };
                                                        document.Insert(++index, propertyValue);
                                                        i = index;
                                                    }
                                                    LogWarning("~~Leaving XamlDisplayer~~");

                                                    hasKey = false;
                                                }
                                            }
                                        }
                                        break;
                                    case BamlRecordType.Property:
                                        {
                                            var propertyRecord = (PropertyRecord)item;
                                            propertyRecord.AttributeId = attributeIdConverter(propertyRecord.AttributeId);
                                        }
                                        break;
                                    case BamlRecordType.PropertyCustom:
                                        {
                                            var customProperty = (PropertyCustomRecord)item;
                                            customProperty.AttributeId = attributeIdConverter(customProperty.AttributeId);
                                        }
                                        break;
                                    case BamlRecordType.PropertyComplexStart:
                                        {
                                            var property = (PropertyComplexStartRecord)item;
                                            property.AttributeId = attributeIdConverter(property.AttributeId);
                                        }
                                        break;
                                    case BamlRecordType.PropertyComplexEnd:

                                        break;
                                    case BamlRecordType.PropertyArrayStart:
                                        {
                                            var property = (PropertyArrayStartRecord)item;
                                            property.AttributeId = attributeIdConverter(property.AttributeId);
                                        }
                                        break;
                                    case BamlRecordType.PropertyArrayEnd:

                                        break;
                                    case BamlRecordType.PropertyListStart:
                                        {
                                            var property = (PropertyListStartRecord)item;
                                            property.AttributeId = attributeIdConverter(property.AttributeId);
                                        }
                                        break;
                                    case BamlRecordType.PropertyListEnd:

                                        break;
                                    case BamlRecordType.PropertyDictionaryStart:
                                        {
                                            var property = (PropertyDictionaryStartRecord)item;
                                            property.AttributeId = attributeIdConverter(property.AttributeId);
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
                                                LogWarning($"~~ Found SMIX Assembly Id {displayerAssemblyId}~~");
                                            }
                                            extra = $"Id: {assembly.AssemblyId} Name: {assembly.AssemblyFullName}";
                                        }
                                        break;
                                    case BamlRecordType.TypeInfo:
                                        {
                                            var typeRecord = (TypeInfoRecord)item;
                                            typeRecord.TypeId = typeIdConverter(typeRecord.TypeId);

                                            extra = $"{typeRecord.TypeFullName} TypeId: {typeRecord.TypeId} AssemblyId: {typeRecord.AssemblyId}";
                                            if (typeRecord.TypeFullName == "ShowMeTheXAML.XamlDisplay" && typeRecord.AssemblyId == displayerAssemblyId)
                                            {
                                                LogWarning($"~~ Found Displayer Id {typeRecord.TypeId}~~");
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
                                            attribRecord.AttributeId = attributeIdConverter(attribRecord.AttributeId);
                                            lastAttributeId = attribRecord.AttributeId;

                                            if (attribRecord.OwnerTypeId == displayerTypeId && attribRecord.Name == "Key")
                                            {
                                                if (elementStack.Count > 0)
                                                {
                                                    hasKey = true;
                                                }
                                                if (keyAttributeId == null)
                                                {
                                                    keyAttributeId = attribRecord.AttributeId;
                                                    LogWarning($"~~ Found Key Id {keyAttributeId}~~");
                                                }
                                                else
                                                {
                                                    LogWarning($"!!!Duplicate key found!!!");
                                                }
                                            }
                                            extra = $"Name: {attribRecord.Name} Id: {attribRecord.AttributeId} OwnerTypeId {attribRecord.OwnerTypeId} Usage {attribRecord.AttributeUsage}";
                                        }
                                        break;
                                    case BamlRecordType.StringInfo:

                                        break;
                                    case BamlRecordType.PropertyStringReference:
                                        {
                                            var property = (PropertyStringReferenceRecord)item;
                                            property.AttributeId = attributeIdConverter(property.AttributeId);
                                        }
                                        break;
                                    case BamlRecordType.PropertyTypeReference:
                                        {
                                            var property = (PropertyTypeReferenceRecord)item;
                                            property.AttributeId = attributeIdConverter(property.AttributeId);
                                        }
                                        break;
                                    case BamlRecordType.PropertyWithExtension:
                                        {
                                            var property = (PropertyWithExtensionRecord)item;
                                            property.AttributeId = attributeIdConverter(property.AttributeId);
                                        }
                                        break;
                                    case BamlRecordType.PropertyWithConverter:
                                        {
                                            var property = (PropertyWithConverterRecord)item;
                                            property.AttributeId = attributeIdConverter(property.AttributeId);
                                            extra = $"AttribId: {property.AttributeId} Value: {property.Value}";
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
                                        {
                                            var lpRecord = (LineNumberAndPositionRecord)item;
                                            extra = $"{lpRecord.LineNumber}, {lpRecord.LinePosition}";
                                        }
                                        break;
                                    case BamlRecordType.LinePosition:

                                        break;
                                    case BamlRecordType.OptimizedStaticResource:

                                        break;
                                    case BamlRecordType.PropertyWithStaticResourceId:
                                        {
                                            var property = (PropertyWithStaticResourceIdRecord)item;
                                            property.AttributeId = attributeIdConverter(property.AttributeId);
                                        }
                                        break;
                                    default:
                                        throw new NotSupportedException();
                                }

                                LogWarning($"{itemIndex} {item.Type} {extra}");
                            }
                            var outStream = new MemoryStream();
                            BamlWriter.WriteDocument(document, outStream);
                            outStream.Position = 0;
                            LogWarning($"Got entry {entry.Key} with stream {outStream.Length}");

                            writer.AddResource((string)entry.Key, outStream);
                        }
                    }
                    writer.Generate();
                    if (replace)
                    {
                        ms.Position = 0;
                        LogWarning($"Write {ms.Length} bytes");
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
}

