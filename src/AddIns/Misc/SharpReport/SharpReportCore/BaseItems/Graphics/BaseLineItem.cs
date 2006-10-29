//------------------------------------------------------------------------------
// <autogenerated>
//     This code was generated by a tool.
//     Runtime Version: 1.1.4322.2032
//
//     Changes to this file may cause incorrect behavior and will be lost if 
//     the code is regenerated.
// </autogenerated>
//------------------------------------------------------------------------------


using System;
using System.Drawing;
using SharpReportCore.Exporters;
/// <summary>
/// This Class handles Lines
/// </summary>
/// <remarks>
/// 	created by - Forstmeier Peter
/// 	created on - 28.09.2005 23:46:19
/// </remarks>
namespace SharpReportCore {
	
	public class BaseLineItem : SharpReportCore.BaseGraphicItem,IExportColumnBuilder {
		
		LineShape shape  = new LineShape();
		
		#region Constructor
		
		public BaseLineItem():base() {
		
		}
		
		#endregion
		
		#region IExportColumnBuilder  implementation
		
		public BaseExportColumn CreateExportColumn(Graphics graphics){	
			BaseStyleDecorator style = base.CreateItemStyle(this.shape);
			ExportGraphic item = new ExportGraphic(style,false);
			return item;
		}
	
		#endregion
		public override void Render(ReportPageEventArgs rpea) {
			if (rpea == null) {
				throw new ArgumentNullException("rpea");
			}
			base.Render (rpea);
			RectangleF rect = base.DrawingRectangle (this.Size);
			shape.DrawShape (rpea.PrintPageEventArgs.Graphics,
			                 new BaseLine (this.ForeColor,base.DashStyle,base.Thickness),
			                 rect);
			
		}
		
		public override string ToString() {
			return "BaseLineItem";
		}
	}
}
