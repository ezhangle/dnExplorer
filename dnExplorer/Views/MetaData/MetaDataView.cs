﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using dnExplorer.Controls;
using dnExplorer.Models;

namespace dnExplorer.Views {
	public class MetaDataView : ViewBase<MetaDataModel> {
		GridView hdrGridView;
		GridView strGridView;
		HexViewer hexView;

		public MetaDataView() {
			var split1 = new SplitContainer {
				Orientation = Orientation.Horizontal,
				Dock = DockStyle.Fill
			};
			Controls.Add(split1);

			var split2 = new SplitContainer {
				Orientation = Orientation.Vertical,
				Dock = DockStyle.Fill
			};
			split1.Panel1.Controls.Add(split2);

			hdrGridView = new GridView();
			hdrGridView.AddColumn(new GridView.Column("Field", true, 120));
			hdrGridView.AddColumn(new GridView.Column("Offset", true));
			hdrGridView.AddColumn(new GridView.Column("Value", false));
			split2.Panel1.Controls.Add(hdrGridView);

			strGridView = new GridView();
			strGridView.AddColumn(new GridView.Column("Offset", false));
			strGridView.AddColumn(new GridView.Column("Size", false));
			strGridView.AddColumn(new GridView.Column("Name", false));
			strGridView.SelectionChanged += OnStreamSelectionChanged;
			split2.Panel2.Controls.Add(strGridView);

			hexView = new HexViewer();
			split1.Panel2.Controls.Add(hexView);

			PerformLayout();

			split2.SplitterDistance = split2.Width / 2;

			hexView.ContextMenuStrip.Items.Add(new ToolStripSeparator());
			var nav = new ToolStripMenuItem("Show in Raw Data");
			nav.Click += OnShowData;
			hexView.ContextMenuStrip.Items.Add(nav);
		}

		void OnStreamSelectionChanged(object sender, EventArgs e) {
			if (strGridView.Rows.Count < 1)
				return;

			object offset, size, name;
			if (strGridView.SelectedCells.Count == 0)
				offset = size = name = "";
			else {
				var cell = strGridView.SelectedCells[0];
				if (cell.RowIndex <= 1 || cell.RowIndex - 2 >= Model.MetaData.MetaDataHeader.Streams)
					offset = size = name = "";
				else {
					var hdr = Model.MetaData.MetaDataHeader.StreamHeaders[cell.RowIndex - 2];
					uint hdrOffset = (uint)hdr.StartOffset;
					offset = hdrOffset + 0;
					size = hdrOffset + 4;
					name = hdrOffset + 8;
				}
			}

			strGridView[0, 1].Value = offset;
			strGridView[1, 1].Value = size;
			strGridView[2, 1].Value = name;
		}

		void PopulateMDHeader() {
			hexView.ClearHighLight();

			var header = Model.MetaData.MetaDataHeader;
			var ranges = new List<Tuple<uint, uint>>();
			uint relBase = (uint)header.StartOffset;
			uint offset = 0;

			hdrGridView.AddRow("Signature", ranges.IncrementOffset(relBase, ref offset, 4),
				header.Signature);

			hdrGridView.AddRow("MajorVersion", ranges.IncrementOffset(relBase, ref offset, 2),
				header.MajorVersion);

			hdrGridView.AddRow("MinorVersion", ranges.IncrementOffset(relBase, ref offset, 2),
				header.MinorVersion);

			hdrGridView.AddRow("Reserved", ranges.IncrementOffset(relBase, ref offset, 4),
				header.Reserved1);

			hdrGridView.AddRow("VersionLength", ranges.IncrementOffset(relBase, ref offset, 4),
				header.StringLength);

			hdrGridView.AddRow("VersionString", ranges.IncrementOffset(relBase, ref offset, header.StringLength),
				header.VersionString);

			hdrGridView.AddRow("Flags", ranges.IncrementOffset(relBase, ref offset, 1),
				header.Flags);

			hdrGridView.AddRow("Reserved", ranges.IncrementOffset(relBase, ref offset, 1),
				header.Reserved2);

			hdrGridView.AddRow("NumberOfStreams", ranges.IncrementOffset(relBase, ref offset, 2),
				header.Streams);

			hexView.AddHighLights(ranges, Color.Red, Color.Blue);
		}

		void PopulateStreamHeader() {
			strGridView.AddRow(new GridView.Cell("", true), new GridView.Cell("", true), new GridView.Cell("", true));

			var ranges = new List<Tuple<uint, uint>>();
			var relBase = (uint)Model.MetaData.MetaDataHeader.StartOffset;
			foreach (var stream in Model.MetaData.MetaDataHeader.StreamHeaders) {
				strGridView.AddRow(stream.Offset, stream.StreamSize, stream.Name);
				var offset = (uint)stream.StartOffset - relBase;
				ranges.Add(Tuple.Create(offset, offset + 8));
				ranges.Add(Tuple.Create(offset + 8, (uint)stream.EndOffset - relBase));
			}
			hexView.AddHighLights(ranges, Color.Orange, Color.Green);
		}

		protected override void OnModelUpdated() {
			hdrGridView.Clear();
			strGridView.Clear();
			if (Model != null) {
				PopulateMDHeader();
				PopulateStreamHeader();
				hexView.Stream = Model.MetaData.PEImage.CreateStream(Model.MetaData.MetaDataHeader);
			}
			else
				hexView.Stream = null;
		}

		void OnShowData(object sender, EventArgs e) {
			var mdHeader = Model.MetaData.MetaDataHeader;

			long begin = (long)mdHeader.StartOffset;
			long end = (long)mdHeader.EndOffset;
			if (hexView.HasSelection) {
				end = begin + hexView.SelectionEnd;
				begin += hexView.SelectionStart;
			}
			ViewUtils.ShowRawData(App, Model, Model.MetaData.PEImage, begin, end);
		}
	}
}