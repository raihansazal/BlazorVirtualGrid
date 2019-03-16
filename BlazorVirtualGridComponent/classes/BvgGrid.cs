﻿using BlazorScrollbarComponent.classes;
using BlazorSplitterComponent;
using BlazorVirtualGridComponent.businessLayer;
using BlazorVirtualGridComponent.ExternalSettings;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static BlazorVirtualGridComponent.classes.BvgEnums;

namespace BlazorVirtualGridComponent.classes
{
    public class BvgGrid<TItem> 
    {
        #region Properties

        #region Actions

        public Action PropertyChanged;

        public Action<string> OnSort { get; set; }

        public Action OnColumnResize { get; set; }

        public Action<int> OnVerticalScroll { get; set; }

        public Action<double> OnHorizontalScroll { get; set; }

        #endregion

        public bool IsReady { get; set; } = false;
        public bool HasMeasuredRect { get; set; } = false;

        public string DivContainerElementID { get; set; } = "DivContainer" + Guid.NewGuid().ToString("d").Substring(1, 4);
        public string Name { get; set; } = "null";

        public double FrozenTableWidth { get; set; } = 0;
        public double NonFrozenTableWidth { get; set; } = 0;
        public double CurrVerticalScrollPosition { get; set; } = 0;
        public double CurrHorizontalScrollPosition { get; set; } = 0;
        public double RowHeightAdjusted { get; set; }

        public int DisplayedRowsCount { get; set; }
        public int DisplayedColumnsCount { get; set; }
        public int RowsTotalCount { get; set; }

        public int[] NonFrozenColwidthSumsByElement { get; set; }

        public Tuple<bool, string, bool> SortState;
        public Tuple<bool, ushort, string> ShouldSelectCell { get; set; } = Tuple.Create(false,(ushort)1,string.Empty);
        public Tuple<bool, ushort, string> ShouldSelectActiveCell { get; set; } = Tuple.Create(false, (ushort)1, string.Empty);

        public PropertyInfo[] AllProps { get; set; }
        public PropertyInfo[] ActiveProps { get; set; }

        public ColProp[] ColumnsOrderedList { get; set; }
        public ColProp[] ColumnsOrderedListFrozen { get; set; }
        public ColProp[] ColumnsOrderedListNonFrozen { get; set; }

        public CssHelper<TItem> cssHelper { get; set; }

        public BvgModal<TItem> bvgModal { get; set; } = new BvgModal<TItem>();

        public BvgRow<TItem>[] Rows { get; set; } = new BvgRow<TItem>[0];
        public BvgColumn<TItem>[] Columns { get; set; } = new BvgColumn<TItem>[0];

        public CompGrid<TItem> compGrid { get; set; }
        public CompBlazorVirtualGrid<TItem> compBlazorVirtualGrid { get; set; }

        public BvgCell<TItem> ActiveCell;
        public BvgRow<TItem> ActiveRow;
        public BvgColumn<TItem> ActiveColumn;
        public BvgSettings<TItem> bvgSettings { get; set; }
        public BvgScroll<TItem> VerticalScroll { get; set; } = null;
        public BvgScroll<TItem> HorizontalScroll { get; set; } = null;
        public BvgSizeDouble bvgSize { get; set; } = new BvgSizeDouble();     
        public BvgPointInt DragStart { get; set; } = new BvgPointInt();
        public BsSettings ResizerBsSettings { get; set; } = new BsSettings();
        public BvgAreaRows<TItem> bvgAreaRowsFrozen { get; set; } = new BvgAreaRows<TItem>();
        public BvgAreaRows<TItem> bvgAreaRowsNonFrozen { get; set; } = new BvgAreaRows<TItem>();
        public BvgAreaColumns<TItem> bvgAreaColumnsFrozen { get; set; } = new BvgAreaColumns<TItem>();
        public BvgAreaColumns<TItem> bvgAreaColumnsNonFrozen { get; set; } = new BvgAreaColumns<TItem>();

        #endregion


        #region Methods

        public void SelectCell(BvgCell<TItem> parCell, bool doFocus)
        {
            ActiveCell = parCell;

            ShouldSelectActiveCell = Tuple.Create(true, parCell.bvgRow.ID, parCell.bvgColumn.prop.Name);

            ActiveRow = parCell.bvgRow;
            ActiveColumn = parCell.bvgColumn;

          
            SelectActiveRow();
            SelectActiveCell(false);

            if (doFocus)
            {
                ActiveCellFocus();
            }
        }

        public void ActiveCellFocus()
        {
            if (ActiveCell != null)
            {
                BvgJsInterop.SetFocus(ActiveCell.ID);
            }
        }


        private void SelectActiveRow()
        {
            Cmd_Clear_Selection();


            ActiveRow.IsSelected = true;

            short j = -1;
            string[] UpdatePkg = new string[ActiveRow.Cells.Count() * 2];
            
 
            foreach (var c in ActiveRow.Cells)
            {
                c.IsSelected = true;
                c.CssClass = CellStyle.CellSelected.ToString();

                UpdatePkg[++j] = c.ID.ToString();
                UpdatePkg[++j] = c.CssClass.ToString();
                //c.InvokePropertyChanged();
            }


            BvgJsInterop.SetAttributeBatch(UpdatePkg, "class");

            //ActiveRow.InvokePropertyChanged();
            //InvokePropertyChanged();
        }

        private void SelectActiveColumn()
        {
            Cmd_Clear_Selection();


            ActiveColumn.IsSelected = true;
            ActiveColumn.CssClass = HeaderStyle.HeaderActive.ToString();

            foreach (var item in Rows)
            {

                BvgCell<TItem> c = item.Cells.Single(x => x.bvgColumn.ID == ActiveColumn.ID);

                c.IsSelected = true;
                c.CssClass = CellStyle.CellSelected.ToString();
                c.InvokePropertyChanged();
            }



            ActiveColumn.BSplitter.SetColor(bvgSettings.HeaderStyle.BackgroundColor);

            ActiveColumn.InvokePropertyChanged();

            

        }

        public void SortColumn(BvgColumn<TItem> parColumn)
        {

            
            if (parColumn.IsSorted)
            {
               
                parColumn.IsAscendingOrDescending = !parColumn.IsAscendingOrDescending;

            }
            else
            {
                foreach (var item in Columns.Where(x => x.IsSorted))
                {
                    item.IsSorted = false;
                    item.InvokePropertyChanged();
                }

                parColumn.IsSorted = true;
                parColumn.IsAscendingOrDescending = true;
            }

            parColumn.InvokePropertyChanged();


            SortState = Tuple.Create(true, parColumn.prop.Name, parColumn.IsAscendingOrDescending);


            if (parColumn.IsAscendingOrDescending)
            {
                OnSort?.Invoke(parColumn.prop.Name);
            }
            else
            {
                OnSort?.Invoke(parColumn.prop.Name + " desc");
            }

        }

        private void SelectActiveCell(bool DoClear = true)
        {
            if (DoClear)
            {
                Cmd_Clear_Selection();
            }

            ActiveCell.IsSelected = true;
            ActiveCell.IsActive = true;
            ActiveCell.CssClass = CellStyle.CellActive.ToString();


            BvgJsInterop.SetAttributeBatch(new string[] { ActiveCell.ID, ActiveCell.CssClass }, "class");


            //ActiveCell.InvokePropertyChanged();
        }


        public void Cmd_Clear_ActiveCellSelection()
        {
            List<string> l = new List<string>();

            foreach (var item in Rows.Where(x => x.Cells.Any(y => y.IsActive)))
            {
                foreach (var cell in item.Cells.Where(x => x.IsActive))
                {
                    cell.IsActive = false;
                    cell.CssClass = CellStyle.CellSelected.ToString();


                    l.Add(cell.ID);
                    l.Add(cell.CssClass);
                }

            }


            BvgJsInterop.SetAttributeBatch(l.ToArray(), "class");



        }

        public void Cmd_Clear_Selection()
        {
            List<string> l = new List<string>();

            foreach (var item in Rows.Where(x=>x.Cells.Any(y=>y.IsSelected)))
            {
               l.AddRange(item.Cmd_Clear_Selection());
            }


            BvgJsInterop.SetAttributeBatch(l.ToArray(), "class");

            //foreach (var item in Columns.Where(x => x.IsSelected))
            //{
            //    item.IsSelected = false;
            //    item.CssClass = HeaderStyle.HeaderRegular.ToString();
            //    item.BSplitter.SetColor(bvgSettings.HeaderStyle.BackgroundColor);
            //    item.InvokePropertyChanged();
            //}

        }

        public ValuesContainer<Tuple<string, ushort>> GetColumnWidths()
        {
            ValuesContainer<Tuple<string, ushort>> result = new ValuesContainer<Tuple<string, ushort>>();

            foreach (var item in ColumnsOrderedList)
            {
                result.Add(Tuple.Create(item.prop.Name, item.ColWidth));
            }


            return result;
        }

        public void InvokePropertyChanged()
        {

            if (PropertyChanged == null)
            {

                if (compGrid != null)
                {
                    compGrid.Subscribe();
                }
            }


            PropertyChanged?.Invoke();
        }

        public void UpdateHorizontalScroll()
        {

            int b = ColumnsOrderedList.Where(x => x.IsFrozen == false).Sum(x => x.ColWidth);


            if (HorizontalScroll.compBlazorScrollbar is null)
            {
                HorizontalScroll.bsbSettings.ScrollTotalSize = b;
                HorizontalScroll.bsbSettings.initialize();

                
            }
            else
            {
                HorizontalScroll.compBlazorScrollbar.SetScrollTotalWidth(b);
            }


        }

        public void CalculateWidths()
        {

            FrozenTableWidth = ColumnsOrderedList.Where(x => x.IsFrozen).Sum(x => x.ColWidth);
            NonFrozenTableWidth = bvgSize.W - FrozenTableWidth;

            //It was more correct to calculate for nonfrozen but when frozen columns resize happens
            // we get different Displayed columns count and should full refresh component, which is slow
            
            //DisplayedColumnsCount = (int)(NonFrozenTableWidth / bvgSettings.ColWidthMin) + 1;
            DisplayedColumnsCount = (int)(bvgSize.W / bvgSettings.ColWidthMin) + 1;

            HorizontalScroll.bsbSettings.ScrollVisibleSize = NonFrozenTableWidth;

            UpdateHorizontalScroll();
        }

        public void AdjustSize()
        {

            DisplayedRowsCount = (int)((bvgSize.H - bvgSettings.HeaderHeight) / bvgSettings.RowHeight);
            

            RowHeightAdjusted =Math.Round((bvgSize.H - bvgSettings.HeaderHeight) / DisplayedRowsCount,3);


            DisplayedRowsCount += 1; //for scroll by pixel effect

            InitializeScrollsAndResizer();

            CalculateWidths();


            bvgAreaRowsFrozen.bvgGrid = this;
            bvgAreaRowsNonFrozen.bvgGrid = this;

            bvgAreaColumnsFrozen.bvgGrid = this;
            bvgAreaColumnsNonFrozen.bvgGrid = this;

           
        }


        public void InitializeScrollsAndResizer()
        {

            VerticalScroll = new BvgScroll<TItem>
            {
                bvgGrid = this,
                bsbSettings = new BsbSettings("VericalScroll")
                {
                    VerticalOrHorizontal = true,
                    width = bvgSettings.ScrollSize,
                    height = bvgSize.H,
                    ScrollVisibleSize = bvgSize.H - bvgSettings.HeaderHeight,
                    ScrollTotalSize = RowsTotalCount * bvgSettings.RowHeight,
                    bsbStyle = new BsbStyle
                    {
                        ButtonColor = bvgSettings.VerticalScrollStyle.ButtonColor,
                        ThumbColor = bvgSettings.VerticalScrollStyle.ThumbColor,
                        ThumbWayColor = bvgSettings.VerticalScrollStyle.ThumbWayColor,
                    }
                }
            };
            VerticalScroll.ID = VerticalScroll.bsbSettings.ID;
            VerticalScroll.bsbSettings.initialize();


            


            HorizontalScroll = new BvgScroll<TItem>
            {
                bvgGrid = this,
                bsbSettings = new BsbSettings("HorizontalScroll")
                {

                    VerticalOrHorizontal = false,
                    width = bvgSize.W,
                    height = bvgSettings.ScrollSize,
                    ScrollVisibleSize = 0,
                    ScrollTotalSize = 0,
                    bsbStyle = new BsbStyle
                    {
                        ButtonColor = bvgSettings.HorizontalScrollStyle.ButtonColor,
                        ThumbColor = bvgSettings.HorizontalScrollStyle.ThumbColor,
                        ThumbWayColor = bvgSettings.HorizontalScrollStyle.ThumbWayColor,
                    }
                }
            };
            HorizontalScroll.ID = HorizontalScroll.bsbSettings.ID;


            ResizerBsSettings = new BsSettings(string.Concat("SplitterResizer"))
            {
                VerticalOrHorizontal = true,
                IsDiagonal = true,
                index = 0,
                width = bvgSettings.ScrollSize,
                height = bvgSettings.ScrollSize,
                BgColor = bvgSettings.VerticalScrollStyle.ThumbColor,
            };

        }

        public void UpdateNonFrozenColwidthSumsByElement()
        {
            ColProp[] c = ColumnsOrderedList.Where(x => x.IsFrozen == false).ToArray();

            NonFrozenColwidthSumsByElement = new int[c.Count()];
            int j = 0;
            for (int i = 0; i < c.Count(); i++)
            {
                j += c[i].ColWidth;
                NonFrozenColwidthSumsByElement[i] = j;

            }

            c = null;
        }


        public void SetScrollTop(double p)
        {

            //double p2 = p;
            //if (LastHorizontalSkip > 0)
            //{
            //    p2 -= bvgGrid.ColumnsOrderedListNonFrozen.Take(LastHorizontalSkip).Sum(x => x.ColWidth);
            //}

            BvgJsInterop.SetElementScrollTop("FrozenDiv1", p);
            BvgJsInterop.SetElementScrollTop("NonFrozenDiv1", p);
        }

        #endregion
    }
}
