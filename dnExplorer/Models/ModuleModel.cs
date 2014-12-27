﻿using System;
using System.Collections.Generic;
using System.Drawing;
using dnExplorer.Trees;
using dnlib.DotNet;
using dnlib.DotNet.MD;

namespace dnExplorer.Nodes {
	public class ModuleModel : LazyModel {
		public IMetaData MetaData { get; set; }
		public ModuleDefMD Module { get; set; }

		public ModuleModel(ModuleDefMD module) {
			Module = module;
			MetaData = module.MetaData;
			Text = module.Name;
		}

		protected override bool HasChildren {
			get { return true; }
		}

		protected override bool IsVolatile {
			get { return false; }
		}

		protected override IEnumerable<IDataModel> PopulateChildren() {
			yield return new PEImageModel(MetaData.PEImage, MetaData.ImageCor20Header);
			if (MetaData != null)
				yield return new MetaDataModel(MetaData);
		}

		public override bool HasIcon {
			get { return true; }
		}

		public override void DrawIcon(Graphics g, Rectangle bounds) {
			g.DrawImageUnscaledAndClipped(Resources.GetResource<Image>("Icons.module.png"), bounds);
		}
	}
}