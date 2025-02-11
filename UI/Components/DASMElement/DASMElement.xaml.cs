﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using smxdasm;
using Xceed.Wpf.AvalonDock.Layout;
using static SPCode.Interop.TranslationProvider;

namespace SPCode.UI.Components;

public partial class DASMElement : UserControl
{
    private double LineHeight = 0.0;
    public new LayoutDocument Parent;
    private SmxFile file_;
    private readonly StringBuilder detail_buffer_ = new();
    public string FilePath;

    public DASMElement()
    {
        InitializeComponent();
    }
    public DASMElement(FileInfo fInfo)
    {
        InitializeComponent();
        LoadFile(fInfo);

        detailbox_.PreviewMouseWheel += PrevMouseWheel;

        detailbox_.Options.EnableHyperlinks = false;
        detailbox_.Options.HighlightCurrentLine = true;
        detailbox_.TextArea.SelectionCornerRadius = 0.0;
        detailbox_.SyntaxHighlighting = new DASMHighlighting();
        FilePath = fInfo.FullName;
    }

    private void LoadFile(FileInfo fInfo)
    {
        try
        {
            using var stream = fInfo.OpenRead();
            using var reader = new BinaryReader(stream);
            file_ = new SmxFile(reader);
        }
        catch (Exception e)
        {
            detailbox_.Text = Translate("ErrorFileLoadProc") + Environment.NewLine + Environment.NewLine + $"{Translate("Details")}: " + e.Message;
            return;
        }
        RenderFile();
    }

    private void PrevMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (LineHeight == 0.0)
        {
            LineHeight = detailbox_.TextArea.TextView.DefaultLineHeight;
        }
        detailbox_.ScrollToVerticalOffset(detailbox_.VerticalOffset - (Math.Sign((double)e.Delta) * LineHeight * Program.OptionsObject.Editor_ScrollLines));
        e.Handled = true;
    }

    private void RenderFile()
    {
        var roots = new Dictionary<string, TreeViewItem>();
        //treeview_.BeginUpdate();
        treeview_.Items.Clear();
        var node = new TreeViewItem() { Header = "(header)" }; //hehe
        treeview_.Items.Add(node);
        var toproot = node;
        node.Tag = new NodeData(RenderFileDetail, null);

        // Add section headers.
        foreach (var section_ in file_.Header.Sections)
        {
            var section = section_;
            var root = new TreeViewItem() { Header = section.Name };
            root.Tag = new NodeData(delegate ()
            {
                RenderSectionHeaderDetail(section);
                EndDetailUpdate();
            }, section);

            roots[section.Name] = root;
            treeview_.Items.Add(root);
        }

        // Add specific sections.
        if (roots.ContainsKey(".natives"))
        {
            RenderNativeList(roots[".natives"], file_.Natives);
        }

        if (roots.ContainsKey(".tags"))
        {
            RenderTagList(roots[".tags"], file_.Tags);
        }

        if (roots.ContainsKey(".pubvars"))
        {
            RenderPubvarList(roots[".pubvars"], file_.Pubvars);
        }

        if (roots.ContainsKey(".publics"))
        {
            RenderPublicsList(roots[".publics"], file_.Publics);
        }

        if (roots.ContainsKey(".code"))
        {
            RenderCodeSection(roots[".code"], file_.CodeV1);
        }

        if (roots.ContainsKey(".data"))
        {
            RenderDataList(roots[".data"], file_.Data);
        }

        if (roots.ContainsKey(".names"))
        {
            RenderNamesList(roots[".names"], file_.Names);
        }

        if (roots.ContainsKey(".dbg.files"))
        {
            RenderDebugFiles(roots[".dbg.files"], file_.DebugFiles);
        }

        if (roots.ContainsKey(".dbg.lines"))
        {
            RenderDebugLines(roots[".dbg.lines"], file_.DebugLines);
        }

        if (roots.ContainsKey(".dbg.info"))
        {
            RenderDebugInfo(roots[".dbg.info"], file_.DebugInfo);
        }

        if (roots.ContainsKey(".dbg.strings"))
        {
            RenderNamesList(roots[".dbg.strings"], file_.DebugNames);
        }

        if (roots.ContainsKey(".dbg.symbols"))
        {
            RenderDebugSymbols(roots[".dbg.symbols"], file_.DebugSymbols);
        }

        if (roots.ContainsKey(".dbg.natives"))
        {
            RenderDebugNatives(roots[".dbg.natives"], file_.DebugNatives);
        }

        RenderFileDetail();
    }

    private void StartDetailUpdate()
    {
        detail_buffer_.Clear();
    }

    private void StartDetail(string fmt, params object[] args)
    {
        StartDetailUpdate();
        AddDetailLine(fmt, args);
    }

    private void AddDetailLine(string fmt, params object[] args)
    {
        detail_buffer_.Append(string.Format(fmt, args) + "\r\n");
    }

    private void EndDetailUpdate()
    {
        detailbox_.Text = detail_buffer_.ToString();
    }

    private void RenderFileDetail()
    {
        StartDetailUpdate();
        AddDetailLine("magic = 0x{0:x}", file_.Header.Magic);
        AddDetailLine("version = 0x{0:x}", file_.Header.Version);
        AddDetailLine("compression = {0} (0x{1:x})", file_.Header.Compression.ToString(), file_.Header.Compression);
        AddDetailLine("disksize = {0} bytes", file_.Header.DiskSize);
        AddDetailLine("imagesize = {0} bytes", file_.Header.ImageSize);
        AddDetailLine("sections = {0}", file_.Header.num_sections);
        AddDetailLine("stringtab = @{0}", file_.Header.stringtab);
        AddDetailLine("dataoffs = @{0}", file_.Header.dataoffs);
        EndDetailUpdate();
    }

    private void RenderSectionHeaderDetail(SectionEntry header)
    {
        StartDetailUpdate();
        AddDetailLine(".nameoffs = 0x{0:x} ; \"{1}\"", header.nameoffs, header.Name);
        AddDetailLine(".dataoffs = 0x{0:x}", header.dataoffs);
        AddDetailLine(".size = {0} bytes", header.Size);
    }

    private void RenderByteView(BinaryReader reader, int size)
    {
        var ndigits = string.Format("{0:x}", size).Length;
        var addrfmt = "0x{0:x" + ndigits + "}: ";

        var chars = new StringBuilder();

        StartDetailUpdate();
        for (var i = 0; i < size; i++)
        {
            if (i % 16 == 0)
            {
                if (i != 0)
                {
                    detail_buffer_.Append("  ");
                    detail_buffer_.Append(chars);
                    detail_buffer_.Append("\r\n");
                    chars.Clear();
                }
                detail_buffer_.Append(string.Format(addrfmt, i));
            }
            else if (i % 8 == 0)
            {
                detail_buffer_.Append(" ");
                chars.Append(" ");
            }

            var value = reader.ReadByte();
            detail_buffer_.Append(string.Format("{0:x2} ", value));

            if (value >= 0x21 && value <= 0x7f)
            {
                chars.Append(Convert.ToChar(value));
            }
            else
            {
                chars.Append(".");
            }
        }
        detail_buffer_.Append("  ");
        detail_buffer_.Append(chars);
        detail_buffer_.Append("\r\n");

        EndDetailUpdate();
    }

    private void RenderHexView(BinaryReader reader, int size)
    {
        var ndigits = string.Format("{0:x}", size).Length;
        var addrfmt = "0x{0:x" + ndigits + "}: ";

        StartDetailUpdate();
        for (var i = 0; i < size; i += 4)
        {
            if (i % 32 == 0)
            {
                if (i != 0)
                {
                    detail_buffer_.Append("  ");
                    detail_buffer_.Append("\r\n");
                }
                detail_buffer_.Append(string.Format(addrfmt, i));
            }
            else if (i % 16 == 0)
            {
                detail_buffer_.Append(" ");
            }

            var value = reader.ReadInt32();
            detail_buffer_.Append(string.Format("{0:x8} ", value));
        }
        EndDetailUpdate();
    }

    private void RenderStringAnalysis(BinaryReader reader, int size)
    {
        StartDetailUpdate();

        var current = new StringBuilder();
        for (var i = 0; i < size; i++)
        {
            var b = reader.ReadByte();
            if (b == 0 && current.Length > 0)
            {
                AddDetailLine("0x{0:x6}: {1}", i, current.ToString());
                current.Clear();
            }

            if (b < 0x20 || b > 0x7f)
            {
                current.Clear();
                continue;
            }

            current.Append(Convert.ToChar(b));
        }
        EndDetailUpdate();
    }

    private void RenderCodeView(SmxCodeV1Section code, string name, int address)
    {
        StartDetailUpdate();

        V1Instruction[] insns;
        try
        {
            insns = V1Disassembler.Disassemble(file_, code, address);
        }
        catch (Exception e)
        {
            AddDetailLine(Translate("NotDissMethod"), name, e.Message);
            EndDetailUpdate();
            return;
        }

        AddDetailLine("; {0}", name);
        AddDetailLine("; {0} instruction(s)", insns.Length);
        AddDetailLine("; starts at code address 0x{0:x}", address);
        AddDetailLine("---");

        if (insns.Length == 0)
        {
            EndDetailUpdate();
            return;
        }


        // Find the largest address so we can get consistent column length.
        var last_address = insns[insns.Length - 1].Address;
        var ndigits = string.Format("{0:x}", last_address).Length;
        var addrfmt = "0x{0:x" + ndigits + "}: ";

        var buffer = new StringBuilder();
        var comment = new StringBuilder();
        foreach (var insn in insns)
        {
            buffer.Clear();
            comment.Clear();
            buffer.Append(insn.Info.Name);

            for (var i = 0; i < insn.Params.Length; i++)
            {
                if (i >= insn.Info.Params.Length)
                {
                    break;
                }

                var kind = insn.Info.Params[i];
                var value = insn.Params[i];

                switch (kind)
                {
                    case V1Param.Constant:
                        buffer.Append(string.Format(" 0x{0:x}", value));
                        comment.Append(string.Format(" {0}", value));
                        break;
                    case V1Param.Native:
                        buffer.Append(string.Format(" {0}", value));
                        if (file_.Natives != null && value < file_.Natives.Length)
                        {
                            comment.Append(string.Format(" {0}", file_.Natives[value].Name));
                        }

                        break;
                    case V1Param.Jump:
                        var delta = value - insn.Address;
                        buffer.Append(string.Format(" 0x{0:x}", value));
                        if (delta >= 0)
                        {
                            comment.Append(string.Format(" +0x{0:x}", delta));
                        }
                        else
                        {
                            comment.Append(string.Format(" -0x{0:x}", -delta));
                        }

                        break;
                    case V1Param.Address:
                    {
                        DebugSymbolEntry sym = null;
                        if (file_.DebugSymbols != null)
                        {
                            sym = file_.DebugSymbols.FindDataRef(value);
                        }

                        buffer.Append(string.Format(" 0x{0:x}", value));
                        if (sym != null)
                        {
                            comment.Append(string.Format(" {0}", sym.Name));
                        }
                        else
                        {
                            comment.Append(string.Format(" {0}", value));
                        }

                        break;
                    }
                    case V1Param.Stack:
                    {
                        DebugSymbolEntry sym = null;
                        if (file_.DebugSymbols != null)
                        {
                            sym = file_.DebugSymbols.FindStackRef(insn.Address, value);
                        }

                        buffer.Append(string.Format(" 0x{0:x}", value));
                        if (sym != null)
                        {
                            comment.Append(string.Format(" {0}", sym.Name));
                        }
                        else
                        {
                            comment.Append(string.Format(" {0}", value));
                        }

                        break;
                    }
                    case V1Param.Function:
                        var fun = file_.FindFunctionName(value);
                        buffer.Append(string.Format(" 0x{0:x}", value));
                        comment.Append(string.Format(" {0}", fun));
                        break;
                }
            }

            detail_buffer_.Append(string.Format(addrfmt, insn.Address));
            detail_buffer_.Append(string.Format("{0,-32}", buffer));
            if (comment.Length > 0)
            {
                detail_buffer_.Append(string.Format(" ;{0}", comment));
            }
            detail_buffer_.Append("\r\n");
        }

        EndDetailUpdate();
    }

    private void RenderCodeSection(TreeViewItem root, SmxCodeV1Section code)
    {
        root.Tag = new NodeData(delegate ()
        {
            RenderSectionHeaderDetail(code.SectionHeader);
            AddDetailLine("codesize = {0} bytes", code.Header.CodeSize);
            AddDetailLine("cellsize = {0} bytes", code.Header.CellSize);
            AddDetailLine("codeversion = 0x{0:x}", code.Header.CodeVersion);
            AddDetailLine("flags = 0x{0:x} ; {0}", code.Header.Flags, code.Header.Flags.ToString());
            AddDetailLine("main = 0x{0:x}", code.Header.main);
            AddDetailLine("codeoffs = 0x{0:x}", code.Header.codeoffs);
            EndDetailUpdate();
        }, code);

        var node = new TreeViewItem() { Header = "cell view" };
        root.Items.Add(node);
        node.Tag = new NodeData(delegate ()
        {
            RenderHexView(code.Reader(), code.Header.CodeSize);
        }, null);

        var functionMap = new Dictionary<string, uint>();

        if (file_.Publics != null)
        {
            foreach (var pubfun in file_.Publics.Entries)
            {
                functionMap[pubfun.Name] = pubfun.Address;
            }
        }
        if (file_.DebugSymbols != null)
        {
            foreach (var sym in file_.DebugSymbols.Entries)
            {
                if (sym.Ident != SymKind.Function)
                {
                    continue;
                }

                functionMap[sym.Name] = sym.CodeStart;
            }
        }

        foreach (var pair in functionMap)
        {
            var name = pair.Key;
            var address = functionMap[pair.Key];
            var snode = new TreeViewItem() { Header = pair.Key };
            root.Items.Add(snode);
            snode.Tag = new NodeData(delegate ()
            {
                RenderCodeView(code, name, (int)address);
            }, null);
        }
    }

    private void RenderDataList(TreeViewItem root, SmxDataSection data)
    {
        root.Tag = new NodeData(delegate ()
        {
            RenderSectionHeaderDetail(data.SectionHeader);
            AddDetailLine("datasize = {0} bytes", data.Header.DataSize);
            AddDetailLine("memory = {0} bytes", data.Header.MemorySize);
            AddDetailLine("dataoffs = 0x{0:x}", data.Header.dataoffs);
            EndDetailUpdate();
        }, data);

        var node = new TreeViewItem() { Header = "byte view" };
        root.Items.Add(node);
        node.Tag = new NodeData(delegate ()
        {
            RenderByteView(data.Reader(), (int)data.Header.DataSize);
        }, null);
        node = new TreeViewItem() { Header = "cell view" };
        root.Items.Add(node);
        node.Tag = new NodeData(delegate ()
        {
            RenderHexView(data.Reader(), (int)data.Header.DataSize);
        }, null);
        node = new TreeViewItem() { Header = "string analysis" };
        root.Items.Add(node);
        node.Tag = new NodeData(delegate ()
        {
            RenderStringAnalysis(data.Reader(), (int)data.Header.DataSize);
        }, null);
    }

    private void RenderPublicsList(TreeViewItem root, SmxPublicTable publics)
    {
        for (var i = 0; i < publics.Length; i++)
        {
            var index = i;
            var pubfun = publics[i];
            var node = new TreeViewItem() { Header = i.ToString() + ": " + pubfun.Name };
            root.Items.Add(node);
            node.Tag = new NodeData(delegate ()
            {
                StartDetail("; public entry {0}", index);
                AddDetailLine("nameoffs = 0x{0:x} ; {1}", pubfun.nameoffs, pubfun.Name);
                AddDetailLine("address = 0x{0:x}", pubfun.Address);
                EndDetailUpdate();
            }, null);
        }
    }

    private void RenderPubvarList(TreeViewItem root, SmxPubvarTable pubvars)
    {
        for (var i = 0; i < pubvars.Length; i++)
        {
            var index = i;
            var pubvar = pubvars[i];
            var node = new TreeViewItem() { Header = i.ToString() + ": " + pubvar.Name };
            root.Items.Add(node);
            node.Tag = new NodeData(delegate ()
            {
                StartDetail("; pubvar entry {0}", index);
                AddDetailLine("nameoffs = 0x{0:x} ; {1}", pubvar.nameoffs, pubvar.Name);
                AddDetailLine("address = 0x{0:x}", pubvar.Address);
                EndDetailUpdate();
            }, null);
        }
    }

    private void RenderTagList(TreeViewItem root, SmxTagTable tags)
    {
        for (var i = 0; i < tags.Length; i++)
        {
            var tag = tags[i];
            var text = tag.Id + ": " + tag.Name;
            if ((tag.Flags & ~TagFlags.Fixed) != 0)
            {
                text += " (" + (tag.Flags & ~TagFlags.Fixed) + ")";
            }

            var node = new TreeViewItem() { Header = text };
            root.Items.Add(node);
            node.Tag = new NodeData(delegate ()
            {
                StartDetail("tag: 0x{0:x} ; flags = {1}", tag.Value, tag.Flags.ToString());
                AddDetailLine("nameoffs: 0x{0:x} ; {1}", tag.entry.nameoffs, tag.Name);
                AddDetailLine("id: 0x{0:x}", tag.Id);
                EndDetailUpdate();
            }, null);
        }
    }

    private void RenderDebugLines(TreeViewItem root, SmxDebugLinesTable lines)
    {
        root.Tag = new NodeData(delegate ()
        {
            RenderSectionHeaderDetail(lines.SectionHeader);
            foreach (var line in lines.Entries)
            {
                AddDetailLine("line {0} @ address 0x{1:x}", line.Line, line.Address);
            }
            EndDetailUpdate();
        }, null);
    }

    private void RenderNativeList(TreeViewItem root, SmxNativeTable natives)
    {
        for (var i = 0; i < natives.Length; i++)
        {
            var index = i;
            var native = natives[i];
            var node = new TreeViewItem() { Header = "[" + i + "] " + native.Name };
            root.Items.Add(node);
            node.Tag = new NodeData(delegate ()
            {
                StartDetail("index = {0}", index);
                AddDetailLine("nameoffs: 0x{0:x} ; {1}", native.nameoffs, native.Name);
                EndDetailUpdate();
            }, null);
        }
    }

    private void RenderNamesList(TreeViewItem root, SmxNameTable names)
    {
        root.Tag = new NodeData(delegate ()
        {
            RenderSectionHeaderDetail(names.SectionHeader);
            foreach (var offset in names.Extents)
            {
                AddDetailLine("0x{0:x}: {1}", offset, names.StringAt(offset));
            }
            EndDetailUpdate();
        }, null);
    }

    private void RenderDebugFiles(TreeViewItem root, SmxDebugFilesTable files)
    {
        root.Tag = new NodeData(delegate ()
        {
            RenderSectionHeaderDetail(files.SectionHeader);
            AddDetailLine("--");
            foreach (var file in files.Entries)
            {
                AddDetailLine("\"{0}\"", file.Name);
                AddDetailLine(" nameoffs = 0x{0:x}", file.nameoffs);
                AddDetailLine(" address = 0x{0:x}", file.Address);
            }
            EndDetailUpdate();
        }, null);
    }

    private void RenderDebugInfo(TreeViewItem root, SmxDebugInfoSection info)
    {
        root.Tag = new NodeData(delegate ()
        {
            RenderSectionHeaderDetail(info.SectionHeader);
            AddDetailLine("num_files = {0}", info.NumFiles);
            AddDetailLine("num_lines = {0}", info.NumLines);
            AddDetailLine("num_symbols = {0}", info.NumSymbols);
            AddDetailLine("num_arrays = {0}", info.NumArrays);
            EndDetailUpdate();
        }, null);
    }

    private string DimsToString(Tag tag, DebugSymbolDimEntry[] dims)
    {
        var str = "";
        for (var i = 0; i < dims.Length; i++)
        {
            int size;
            if (i == dims.Length - 1 && tag != null && tag.Name == "String")
            {
                size = dims[i].Size * 4;
            }
            else
            {
                size = dims[i].Size;
            }

            if (size == 0)
            {
                str += "[]";
            }
            else
            {
                str += string.Format("[{0}]", size);
            }
        }
        return str;
    }

    private void RenderSymbolDetail(DebugSymbolEntry entry)
    {
        Tag tag = null;
        if (file_.Tags != null)
        {
            tag = file_.Tags.FindTag(entry.TagId);
        }

        StartDetail("; {0}", entry.Name);
        if (entry.Address < 0)
        {
            AddDetailLine("address = -0x{0:x}", -entry.Address);
        }
        else
        {
            AddDetailLine("address = 0x{0:x}", entry.Address);
        }

        if (tag == null)
        {
            AddDetailLine("tagid = 0x{0:x}", entry.TagId);
        }
        else
        {
            AddDetailLine("tagid = 0x{0:x} ; {1}", entry.TagId, tag.Name);
        }

        AddDetailLine("codestart = 0x{0:x}", entry.CodeStart);
        AddDetailLine("codeend = 0x{0:x}", entry.CodeEnd);
        AddDetailLine("nameoffs = 0x{0:x} ; {1}", entry.nameoffs, entry.Name);
        AddDetailLine("kind = {0:d} ; {1}", entry.Ident, entry.Ident.ToString());
        AddDetailLine("scope = {0:d} ; {1}", entry.Scope, entry.Scope.ToString());

        if (entry.Dims != null)
        {
            AddDetailLine("dims = {0}", DimsToString(tag, entry.Dims));
        }

        string file = null;
        if (file_.DebugFiles != null)
        {
            file = file_.DebugFiles.FindFile(entry.CodeStart);
        }

        if (file != null)
        {
            AddDetailLine("file: \"{0}\"", (string)file);
        }

        uint? line = null;
        if (file_.DebugLines != null)
        {
            line = file_.DebugLines.FindLine(entry.CodeStart);
        }

        if (line != null)
        {
            AddDetailLine("line: \"{0}\"", (uint)line);
        }

        EndDetailUpdate();
    }

    private void RenderDebugFunction(SmxDebugSymbolsTable syms, TreeViewItem root, DebugSymbolEntry fun)
    {
        root.Tag = new NodeData(delegate ()
        {
            RenderSymbolDetail(fun);
        }, null);

        var args = new List<DebugSymbolEntry>();
        var locals = new List<DebugSymbolEntry>();
        foreach (var sym_ in syms.Entries)
        {
            var sym = sym_;
            if (sym.Scope == SymScope.Global)
            {
                continue;
            }

            if (sym.CodeStart < fun.CodeStart || sym.CodeEnd > fun.CodeEnd)
            {
                continue;
            }

            if (sym.Address < 0)
            {
                locals.Add(sym);
            }
            else
            {
                args.Add(sym);
            }
        }

        args.Sort(delegate (DebugSymbolEntry e1, DebugSymbolEntry e2)
        {
            return e1.Address.CompareTo(e2.Address);
        });
        foreach (var sym_ in args)
        {
            var sym = sym_;
            var node = new TreeViewItem() { Header = sym.Name };
            root.Items.Add(node);
            node.Tag = new NodeData(delegate ()
            {
                RenderSymbolDetail(sym);
            }, null);
        }

        locals.Sort(delegate (DebugSymbolEntry e1, DebugSymbolEntry e2)
        {
            return e1.CodeStart.CompareTo(e2.CodeStart);
        });
        foreach (var sym_ in locals)
        {
            var sym = sym_;
            var node = new TreeViewItem() { Header = sym.Name };
            root.Items.Add(node);
            node.Tag = new NodeData(delegate ()
            {
                RenderSymbolDetail(sym);
            }, null);
        }
    }

    private void RenderDebugSymbols(TreeViewItem root, SmxDebugSymbolsTable syms)
    {
        var globals = root.Items.Add("globals");
        foreach (var sym_ in syms.Entries)
        {
            var sym = sym_;
            if (sym.Scope != SymScope.Global)
            {
                continue;
            }

            if (sym.Ident == SymKind.Function)
            {
                continue;
            }

            var node = new TreeViewItem() { Header = sym.Name };
            root.Items.Add(node);
            node.Tag = new NodeData(delegate ()
            {
                RenderSymbolDetail(sym);
            }, null);
        }

        var functions = root.Items.Add("functions");
        foreach (var sym_ in syms.Entries)
        {
            var sym = sym_;
            if (sym.Scope != SymScope.Global)
            {
                continue;
            }

            if (sym.Ident != SymKind.Function)
            {
                continue;
            }

            var node = new TreeViewItem() { Header = sym.Name };
            root.Items.Add(node);
            RenderDebugFunction(syms, node, sym);
        }
    }

    private void RenderDebugNative(DebugNativeEntry entry)
    {
        Tag tag = null;
        if (file_.Tags != null)
        {
            tag = file_.Tags.FindTag(entry.tagid);
        }

        StartDetailUpdate();
        AddDetailLine("nameoffs = 0x{0:x}", entry.nameoffs, entry.Name);
        if (tag == null)
        {
            AddDetailLine("tagid = 0x{0:x}", entry.tagid);
        }
        else
        {
            AddDetailLine("tagid = 0x{0:x} ; {1}", entry.tagid, entry.Name);
        }

        AddDetailLine("index = {0}", entry.Index);
        for (var i = 0; i < entry.Args.Length; i++)
        {
            var arg = entry.Args[i];
            AddDetailLine("arg {0}", i);
            AddDetailLine("  nameoffs = 0x{0:x} ; {1}", arg.nameoffs, arg.Name);
            AddDetailLine("  kind = {0:d} ; {1}", arg.Ident, arg.Ident.ToString());

            if (arg.Dims != null)
            {
                AddDetailLine("  dims = {0}", DimsToString(tag, arg.Dims));
            }
        }
        EndDetailUpdate();
    }

    private void RenderDebugNatives(TreeViewItem root, SmxDebugNativesTable natives)
    {
        foreach (var native_ in natives.Entries)
        {
            var native = native_;
            var node = new TreeViewItem() { Header = native.Name };
            root.Items.Add(node);
            node.Tag = new NodeData(delegate ()
            {
                RenderDebugNative(native);
            }, null);
        }
    }

    private void Treeview__SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        var node = treeview_.SelectedItem;
        if (node is TreeViewItem item)
        {
            if (item.Tag == null)
            {
                return;
            }
            var data = (NodeData)item.Tag;
            if (data.callback == null)
            {
                return;
            }
            data.callback();
        }
        return;
    }

    public delegate void DrawNodeFn();

    public void Close()
    {
        Program.MainWindow.DockingPane.RemoveChild(Parent);
        Program.MainWindow.DASMReferences.Remove(this);
        Program.RecentFilesStack.Push(FilePath);
        Program.MainWindow.UpdateWindowTitle();
    }

    public class NodeData
    {
        public DrawNodeFn callback;
        public object data;

        public NodeData(DrawNodeFn aCallback, object aData)
        {
            callback = aCallback;
            data = aData;
        }
    }
}