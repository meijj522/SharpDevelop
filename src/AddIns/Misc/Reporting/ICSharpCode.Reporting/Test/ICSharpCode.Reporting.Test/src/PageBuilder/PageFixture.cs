﻿/*
 * Created by SharpDevelop.
 * User: Peter Forstmeier
 * Date: 24.04.2013
 * Time: 19:55
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;
using System.Reflection;
using System.Drawing;
using ICSharpCode.Reporting.Globals;
using ICSharpCode.Reporting.Interfaces;
using ICSharpCode.Reporting.Interfaces.Export;
using NUnit.Framework;

namespace ICSharpCode.Reporting.Test.PageBuilder
{
	[TestFixture]
	public class PageFixture
	{
		
		private IReportCreator reportCreator;
		
		[Test]
		public void CreateGraphicsFromPageSize () {
			reportCreator.BuildExportList();
			var page = reportCreator.Pages[0];
			Graphics g = CreateGraphics.FromSize(page.Size);
			Assert.That(g,Is.Not.Null);
		}
//	http://www.dev102.com/2008/10/09/measure-string-size-in-pixels-c/
		//http://www.codeproject.com/Articles/2118/Bypass-Graphics-MeasureString-limitations
		//http://codebetter.com/patricksmacchia/2009/08/31/reveal-hidden-api-usage-tricks-from-any-net-application/
		

		[Test]
		public void GraphicsIsSameSizeAsPage() {
			reportCreator.BuildExportList();
			var page = reportCreator.Pages[0];
			var graphics = CreateGraphics.FromSize(page.Size);
			Assert.That(graphics.VisibleClipBounds.Width,Is.EqualTo(page.Size.Width));
			Assert.That(graphics.VisibleClipBounds.Height,Is.EqualTo(page.Size.Height));
		}
		
		#region PageInfo
		
		[Test]
		public void PageInfoPageNumberIsOne() {
			reportCreator.BuildExportList();
			var pageInfo = reportCreator.Pages[0].PageInfo;
			Assert.That(pageInfo.PageNumber,Is.EqualTo(1));
		}

		
		[Test]
		public void PageInfoReportName() {
			reportCreator.BuildExportList();
			var pi = reportCreator.Pages[0].PageInfo;
			Assert.That(pi.ReportName,Is.EqualTo("Report1"));
		}
		
		
		#endregion
		
		[SetUp]
		public void LoadFromStream()
		{
			Assembly asm = Assembly.GetExecutingAssembly();
			var stream = asm.GetManifestResourceStream(TestHelper.RepWithTwoItems);
			var reportingFactory = new ReportingFactory();
			reportCreator = reportingFactory.ReportCreator(stream);
		}
	}
}
