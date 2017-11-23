
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

                            Stack<BamlRecord> elementStack = new Stack<BamlRecord>();
                            ushort? displayerTypeId = null;
                            int i = 0;
                            bool inDisplayer = false;
                            foreach (BamlRecord item in document)
                            {
                                string extra = "";
                                if (item.Type == BamlRecordType.LineNumberAndPosition &&
                                    item is LineNumberAndPositionRecord lpRecord)
                                {
                                    extra = $"{lpRecord.LineNumber}, {lpRecord.LinePosition}";
                                }
                                else if (item.Type == BamlRecordType.TypeInfo && item is TypeInfoRecord typeRecord)
                                {
                                    extra = $"{typeRecord.TypeFullName} TypeId {typeRecord.TypeId}";
                                    if (typeRecord.TypeFullName == "ShowMeTheXAML.XamlDisplay")
                                    {
                                        displayerTypeId = typeRecord.TypeId;
                                    }
                                }
                                else if (item.Type == BamlRecordType.ElementStart &&
                                         item is ElementStartRecord startRecord)
                                {
                                    extra = $" TypeId {startRecord.TypeId}";
                                    if (startRecord.TypeId == displayerTypeId)
                                    {
                                        elementStack.Push(item);
                                        LogWarning("~~Found XamlDisplayer~~");
                                    }
                                }
                                else if (item.Type == BamlRecordType.AttributeInfo &&
                                         item is AttributeInfoRecord attribRecord)
                                {
                                    extra = $"Name: {attribRecord.Name} Id: {attribRecord.AttributeId} OwnerTypeId {attribRecord.OwnerTypeId} Usage {attribRecord.AttributeUsage}";
                                }
                                else if (item.Type == BamlRecordType.ElementEnd && item is ElementEndRecord endRecord)
                                {
                                    if (elementStack.Count > 0)
                                    {
                                        elementStack.Pop();
                                        if (elementStack.Count == 0)
                                        {
                                            LogWarning("~~Leaving XamlDisplayer~~");
                                        }
                                    }
                                }
                                else if (item.Type == BamlRecordType.PropertyWithConverter && item is PropertyRecord pwcRecord)
                                {
                                    extra = $"AttribId: {pwcRecord.AttributeId} Value: {pwcRecord.Value}";
                                }
                                LogWarning($"{i++} {item.Type} {extra}");
                            }
                            var outStream = new MemoryStream();
                            BamlWriter.WriteDocument(document, outStream);
                            outStream.Position = 0;
                            LogWarning($"Got entry {entry.Key} with stream {outStream.Length}");

                            writer.AddResource("2" + (string)entry.Key, outStream);
                        }
                    }
                    writer.Generate();
                    if (replace)
                    {
                        ms.Position = 0;
                        LogWarning($"Write {ms.Length} bytes");
                        ModuleDefinition.Resources.Add(new EmbeddedResource("ShowMeTheXAML2.g.resources", resource.Attributes,
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

