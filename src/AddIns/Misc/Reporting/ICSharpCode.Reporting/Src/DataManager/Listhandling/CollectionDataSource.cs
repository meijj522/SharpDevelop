﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

using ICSharpCode.Reporting.BaseClasses;
using ICSharpCode.Reporting.DataSource;
using ICSharpCode.Reporting.Interfaces;
using ICSharpCode.Reporting.Interfaces.Data;
using ICSharpCode.Reporting.Items;

namespace ICSharpCode.Reporting.DataManager.Listhandling
{
	/// <summary>
	/// Description of DataSource.
	/// </summary>
	public enum OrderGroup {
		AsIs,
		Sorted,
		Grouped
	}
	
	public class CollectionDataSource:IDataSource
	{
		readonly DataCollection<object> baseList;
		readonly ReportSettings reportSettings;
		readonly Type elementType;
		readonly PropertyDescriptorCollection listProperties;

	
		public CollectionDataSource(IEnumerable list, ReportSettings reportSettings)
		{
			if (list == null)
				throw new ArgumentNullException("list");
			if (reportSettings == null)
				throw new ArgumentNullException("reportSettings");
			baseList = CreateBaseList(list);
			CurrentList = baseList;
			
			this.reportSettings = reportSettings;
			this.listProperties = this.baseList.GetItemProperties(null);
			OrderGroup = OrderGroup.AsIs;
		}
		
		
		[Obsolete("use public CollectionDataSource(IEnumerable list, ReportSettings reportSettings")]
		public CollectionDataSource(IEnumerable list, Type elementType, ReportSettings reportSettings)
		{
			if (list == null)
				throw new ArgumentNullException("list");
			if (reportSettings == null)
				throw new ArgumentNullException("reportSettings");
			
			baseList = CreateBaseList(list);
			
			CurrentList = baseList;
			
			this.elementType = elementType;
			this.reportSettings = reportSettings;
			
			this.listProperties = this.baseList.GetItemProperties(null);
			OrderGroup = OrderGroup.AsIs;
		}
		
		
		
		public Collection<AbstractColumn> AvailableFields {
			get {
				var availableFields = new Collection<AbstractColumn>();
				foreach (PropertyDescriptor p in this.listProperties){
					availableFields.Add (new AbstractColumn(p.Name,p.PropertyType));
				}
				return availableFields;
			}
		}
		
		
		public IList <object> CurrentList {get;private set;}
		
		
		public int Count {
			get {return baseList.Count;}	
		}
		
		
		public object Current {get; private set;}
		
		public OrderGroup OrderGroup {get; private set;}
		
		
		public IGrouping<object, object> CurrentKey {get; private set; }
		
		
		#region Sort
		
		void Sort()
		{
			if (reportSettings.SortColumnsCollection.Count > 0) {
				var sorted = SortInternal();
				OrderGroup = OrderGroup.Sorted;
				listEnumerator = sorted.GetEnumerator();
			} else {
				OrderGroup = OrderGroup.AsIs;
				listEnumerator = baseList.GetEnumerator();
			}
			listEnumerator.MoveNext();
			Current = listEnumerator.Current;
		}
		
		
		IEnumerable<object> SortInternal (){
			IEnumerable<object> sortedList = null;
			var sortProperty = listProperties.Find(reportSettings.SortColumnsCollection[0].ColumnName,true);
			if(reportSettings.SortColumnsCollection.Count == 1) {
				sortedList = baseList.OrderBy(o => o.GetType().GetProperty(sortProperty.Name).GetValue(o, null) );
				
			}
			return sortedList;
		}
		
		#endregion
		
		
		#region Grouping
		
		void Group()
		{
			OrderGroup = OrderGroup.Grouped;
			groupedList = GroupInternal();
			groupEnumerator = groupedList.GetEnumerator();
			groupEnumerator.MoveNext();
			CurrentKey = groupEnumerator.Current;
			listEnumerator = groupEnumerator.Current.GetEnumerator();
			listEnumerator.MoveNext();
			Current = listEnumerator.Current;
		}
		
		
		IEnumerable<IGrouping<object, object>> GroupInternal () {
			var property = listProperties.Find(reportSettings.GroupColumnsCollection[0].ColumnName,true);
			var sortProperty = listProperties.Find("Randomint",true);

			var groupedList = baseList.OrderBy(o => o.GetType().GetProperty(sortProperty.Name).GetValue(o, null) )
				.GroupBy(a => a.GetType().GetProperty(property.Name).GetValue(a, null)).OrderBy(c => c.Key);
			return groupedList;
		}
		
		#endregion
		
		public void Bind()
		{
			if (reportSettings.GroupColumnsCollection.Any()) {
				Group();
			} else {
				Sort();
			}
		}
		
		#region Fill
		
		
		IEnumerator<IGrouping<object, object>> groupEnumerator;
		
		
		IEnumerable<IGrouping<object, object>> groupedList;
		
		public IEnumerable<IGrouping<object, object>> GroupedList {
			get { return groupedList; }
		}
		
		IEnumerator<object> listEnumerator;
		
		public void Fill(List<IPrintableObject> collection)
		{
			foreach (var element in collection) {
				var container = element as ReportContainer;
				if (container != null) {
					FillFromList(container.Items);
				} else {
					FillInternal(element);
				}
			}
		}

		void FillFromList(List<IPrintableObject> collection)
		{
			foreach (IPrintableObject item in collection) {
				FillInternal(item);
			}
		}


		void FillInternal(IPrintableObject item)
		{
			var dbItem = item as IDataItem;
			if (dbItem != null) {
				dbItem.DBValue = String.Empty;
				dbItem.DBValue = ReadValueFromProperty(dbItem.ColumnName);
				if (String.IsNullOrEmpty(dbItem.DataType)) {
					dbItem.DataType = SetTypeFromProperty(dbItem.ColumnName).ToString();
				}
			}
		}
		
		
		string ReadValueFromProperty (string columnName) {
			var propertyPath = Current.ParsePropertyPath(columnName);
			var val = propertyPath.Evaluate(Current);
			return val.ToString();
		}
		
		
		Type SetTypeFromProperty (string columnName) {
			var p = listProperties.Find(columnName,true);
			return p.PropertyType;
		}
		
		
		public bool MoveNext()
		{
			var canMove = listEnumerator.MoveNext();
		
			if (OrderGroup == OrderGroup.Grouped) {
				if (! canMove) {
					var groupCanMove = groupEnumerator.MoveNext();
					CurrentKey = groupEnumerator.Current;
					if (groupCanMove) {
						listEnumerator = groupEnumerator.Current.GetEnumerator();
						canMove = listEnumerator.MoveNext();
					}
				}
				Current = listEnumerator.Current;
				return canMove;
			}
			
			Current = listEnumerator.Current;
			return canMove;
		}
		
		#endregion
		
		
		static DataCollection<object> CreateBaseList(IEnumerable source)
		{
			Type elementType = source.AsQueryable().ElementType;
			var list = new DataCollection<object>(elementType);
			list.AddRange(source);
			return list;
		}
	}
}
