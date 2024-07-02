using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ERP6.Models;
using Microsoft.AspNetCore.Http;
using ERP6.ViewModels.Sales;
using X.PagedList;
using System.IO;
using ERP6.Helpers;
using System.Text;

namespace ERP6.Controllers
{
    public class SalesController : Controller
    {
        private readonly EEPEF01Context _context;

        private AjaxsController _ajaxController;

        public SalesController(EEPEF01Context context, AjaxsController ajaxController)
        {
            _context = context;
            _ajaxController = ajaxController;
        }

        // 獲取此客戶與此產品的價格
        public async Task<IActionResult> Area()
        {
            try
            {
                var area = _context.STO_AREA.Where(x => x.AREA_STATE == 1).ToList();
                ViewBag.Area = area;

                return View(await _context.Stock10.Take(30).ToListAsync());
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IActionResult> Order(OrderViewModel vm, int? page = 1)
        {
            try
            {
                vm.OrderState = vm.OrderState ?? "4";

                // 只保存一個禮拜的資料
                var Today = DateTime.Today.ToString("yyyyMMdd") + "2359"; // 今天
                var BeforeAWeek = DateTime.Today.AddDays(-6).ToString("yyyyMMdd") + "0000"; // 一個禮拜
                //所有一個禮拜內的訂單
                var orderData = await _context.Out10.Join(_context.Cstm10, x => x.CoNo, y => y.CoNo, (x, y) => new OrderList
                {
                    OrderNo = x.OutNo ?? "",
                    OrderState = x.OutType ?? "",
                    KeyInDate = x.KeyinDate,
                    OrderTime = !String.IsNullOrEmpty(x.KeyinDate) ? DateTime.ParseExact((x.KeyinDate).PadRight(12, '0').ToString(), "yyyyMMddHHmm", null).ToString("yyyy-MM-dd HH:mm") : "",
                    OrderAmount = (double)Math.Round((decimal)x.Total, 2),
                    CustomerId = x.CoNo ?? "",
                    CoName = y.Coname ?? "",
                    Area = y.AreaNo ?? "",
                }).Where(z => z.KeyInDate != ""
                         && String.Compare(z.KeyInDate, Today) <= 0
                         && String.Compare(z.KeyInDate, BeforeAWeek) >= 0
                         ).OrderByDescending(z => z.OrderNo).ToListAsync();


                // 搜尋條件 - 訂單狀態
                if (!String.IsNullOrEmpty(vm.OrderState) && vm.OrderState != "4")
                {
                    orderData = await orderData.Where(x => x.OrderState == vm.OrderState).ToListAsync();
                }

                // 搜尋條件 - 訂單編號
                if (!String.IsNullOrEmpty(vm.OrderNo))
                {
                    orderData = await orderData.Where(x => x.OrderNo == vm.OrderNo).ToListAsync();
                }

                // 搜尋條件 - 訂購時間（建檔日期）
                if (!String.IsNullOrEmpty(vm.OrderTime))
                {
                    var keyindate = DateTime.ParseExact(vm.OrderTime, "yyyy-MM-dd", null).ToString("yyyyMMdd");
                    orderData = await orderData.Where(x => (x.KeyInDate).Substring(0, 8) == keyindate).ToListAsync();
                }

                // 搜尋條件 - 區域
                if (!String.IsNullOrEmpty(vm.Area))
                {
                    orderData = await orderData.Where(x => x.Area == vm.Area).ToListAsync();
                }

                // 搜尋條件 - 客戶編號
                if (!String.IsNullOrEmpty(vm.CustomerId))
                {
                    orderData = await orderData.Where(x => x.CustomerId == vm.CustomerId).ToListAsync();
                }

                #region selected

                ViewBag.Area = new SelectList(_context.STO_AREA.Where(x => x.AREA_STATE == 1).ToList(), "AREA_NAME", "AREA_NAME", vm.Area);
                ViewBag.Client = new SelectList(_context.Cstm10.Where(x => x.AreaNo == vm.Area).ToList(), "CoNo", "Coname", string.Empty);

                var orderStatsList = new List<SelectListItem>()
                {
                   new SelectListItem {Text="全部訂單", Value="4" },
                   new SelectListItem {Text="未出貨訂單", Value="2" },
                   new SelectListItem {Text="已出貨訂單", Value="1" },
                };

                ViewBag.StateSList = new SelectList(orderStatsList, "Value", "Text", vm.OrderState);

                #endregion selected

                if (vm.IsSearch)
                {
                    vm.Shipped = orderData.Where(x => x.OrderState == "1").Count();
                    vm.NotShipped = orderData.Where(x => x.OrderState == "2").Count();
                    vm.orderList = await orderData.ToPagedListAsync((int)page, 20);
                }
                else
                {
                    orderData = orderData.Where(x => (x.KeyInDate).Substring(0, 8) == DateTime.Today.ToString("yyyyMMdd")).ToList();
                    vm.Shipped = orderData.Where(x => x.OrderState == "1").Count();
                    vm.NotShipped = orderData.Where(x => x.OrderState == "2").Count();
                    vm.orderList = await orderData.ToPagedListAsync((int)page, orderData.Count() + 1);
                }


                return View(vm);
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public async Task<IActionResult> OrderNotUse(OrderViewModel vm, int? page = 1)
        {
            try
            {
                #region new

                var orderData = await (from out10 in _context.Out10
                                       join cstm10 in _context.Cstm10 on out10.CoNo equals cstm10.CoNo
                                       where cstm10.AreaNo == vm.Area
                                       orderby out10.OutNo descending
                                       select new
                                       {
                                           OrderNo = out10.OutNo ?? "",
                                           OrderState = out10.OutType ?? "",
                                           //OrderTime = DateTime.ParseExact(out10.OutDate, "yyyyMMdd", null).ToString("yyyy-MM-dd") ?? "",
                                           OrderTime = !String.IsNullOrEmpty(out10.OutDate) ? DateTime.ParseExact(out10.OutDate, "yyyyMMdd", null).ToString("yyyy-MM-dd") : "",
                                           OrderAmount = (double)Math.Round((decimal)out10.Total, 2),
                                           CoNo = out10.CoNo ?? "",
                                           CustomerId = out10.CoNo ?? "",
                                           CoName = cstm10.Coname ?? "",
                                           //KeyInDate = DateTime.ParseExact(out10.KeyinDate, "yyyyMMdd", null).ToString("yyyy-MM-dd"),
                                           //Business = _context.Pepo10.Where(y => y.PeNo == out10.PeNo).FirstOrDefault().Name,
                                           //Driver = _context.Pepo10.Where(y => y.PeNo == out10.DriveNo).FirstOrDefault().Name,
                                           OutType = out10.OutType ?? "",
                                           //PeNo = out10.PeNo,
                                           //DriveNo = out10.DriveNo,
                                       }).ToListAsync();



                //var orderData = _context.Out10
                //  .Where(x => x.CoNo == vm.CustomerId)
                //  .OrderByDescending(x => x.OutDate);

                if (orderData == null)
                {
                    return NotFound();
                }

                var data = new OrderViewModel
                {
                    //Shipped = orderData.Where(x => x.OutType == "1").Count(),
                    //NotShipped = orderData.Where(x => x.OutType == "2").Count(),
                    //OrderStatus 預設給4(全部訂單)
                    OrderState = string.IsNullOrEmpty(vm.OrderState) ? "4" : vm.OrderState,
                    Area = vm.Area,
                    //CustomerId = vm.CustomerId,
                };

                #region selected

                ViewBag.Area = new SelectList(_context.STO_AREA.Where(x => x.AREA_STATE == 1).ToList(), "AREA_NAME", "AREA_NAME", data.Area);
                ViewBag.Client = new SelectList(_context.Cstm10.Where(x => x.AreaNo == vm.Area).ToList(), "CoNo", "Company", string.Empty);

                var orderStatsList = new List<SelectListItem>()
                {
                   new SelectListItem {Text="全部訂單", Value="4" },
                   new SelectListItem {Text="未出貨訂單", Value="2" },
                   new SelectListItem {Text="已出貨訂單", Value="1" },
                };

                ViewBag.StateSList = new SelectList(orderStatsList, "Value", "Text", data.OrderState);

                //業務人員
                //ViewBag.BusinessList = new SelectList(_context.Pepo10.Where(x => x.Dep == "業務部").ToList(), "PeNo", "Name", data.Business);

                //司機人員
                //ViewBag.DriverList = new SelectList(_context.Pepo10.Where(x => x.Posi == "司機").ToList(), "PeNo", "Name", data.Driver);

                #endregion

                ////沒有資料時退回選擇區域客戶畫面
                //if (string.IsNullOrEmpty(vm.CustomerId))
                //{
                //    return RedirectToAction("Area", "Sales");
                //}

                var orderList = await orderData.Select(x => new OrderList
                {
                    OrderNo = x.OrderNo,
                    OrderState = x.OrderState,
                    OrderTime = x.OrderTime,
                    OrderAmount = (double)Math.Round((decimal)x.OrderAmount, 2),
                    CustomerId = x.CoNo,
                    CoName = x.CoName,
                    //KeyInDate = x.KeyInDate,
                    ////Business = _context.Pepo10.Where(y => y.PeNo == x.PeNo).FirstOrDefault().Name,
                    ////Driver = _context.Pepo10.Where(y => y.PeNo == x.DriveNo).FirstOrDefault().Name,
                    OutType = x.OutType,
                }).ToListAsync();

                //搜尋條件-狀態
                if (!string.IsNullOrEmpty(data.OrderState) && data.OrderState != "4")
                {
                    orderList = await orderList.Where(x => x.OrderState == vm.OrderState).ToListAsync();
                }

                //搜尋條件-訂單時間
                if (!string.IsNullOrEmpty(vm.OrderTime))
                {
                    orderList = await orderList.Where(x => x.OrderTime.Contains(vm.OrderTime)).ToListAsync();
                }

                //搜尋條件-客戶編號
                if (!string.IsNullOrEmpty(vm.CustomerId))
                {
                    orderList = await orderList.Where(x => x.CustomerId == vm.CustomerId).ToListAsync();
                }

                //搜尋條件-訂單編號
                if (!string.IsNullOrEmpty(vm.OrderNo))
                {
                    orderList = await orderList.Where(x => x.OrderNo.Contains(vm.OrderNo)).ToListAsync();
                }

                //搜尋條件-建檔日期
                if (!string.IsNullOrEmpty(vm.KeyInDate))
                {
                    orderList = await orderList.Where(x => x.KeyInDate.Contains(vm.KeyInDate)).ToListAsync();
                }

                //搜尋條件-業務人員
                //if (!string.IsNullOrEmpty(vm.Business))
                //{
                //    orderList = orderList.Where(x => x.Business.Contains(vm.Business)).ToList();
                //}

                //搜尋條件-司機人員
                //if (!string.IsNullOrEmpty(vm.Driver))
                //{
                //    orderList = orderList.Where(x => x.Driver.Contains(vm.Driver)).ToList();
                //}

                data.orderList = await orderList.ToPagedListAsync((int)page, 500);

                if (orderData == null)
                {
                    return View(vm);
                }

                #endregion
                data.Shipped = orderList.Where(x => x.OutType == "1").Count();
                data.NotShipped = orderList.Where(x => x.OutType == "2").Count();

                return View(data);
            }
            catch (Exception e)
            {

                throw;
            }
        }

        public async Task<IActionResult> OrderAdd(string Area, string CustomerId)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            try
            {
                var vm = new OrderAddViewModel();

                var CSTM = _context.Cstm10.Find(CustomerId);

                // 抓取上筆訂單的紀錄
                var LastOut = !String.IsNullOrEmpty(CustomerId) ?
                    await _context.Out10.Where(x => x.CoNo == CustomerId && x.OutType == "1").OrderByDescending(x => x.OutNo).FirstOrDefaultAsync() : null;

                // 抓取上筆訂單的明細紀錄
                var LastOut20 = !String.IsNullOrEmpty(CustomerId) ? _context.Out20.Join(_context.Out10.Where(x => x.CoNo == CustomerId && x.OutType == "1")
                    , x => x.OutNo, y => y.OutNo, (x, y) => new { PartNo = x.PartNo, Qty = x.Qty }).Where(x => x.Qty > 0).Select(x => x.PartNo).Distinct().ToList() : new List<string>();
                //var LastOut20 = LastOut != null ? await _context.Out20.Where(x => x.OutNo == LastOut.OutNo).Select(x => x.PartNo).Distinct().ToListAsync() : new List<string>();

                vm.OutNo = "";
                vm.Area = Area ?? "";
                vm.CoNo = CustomerId ?? "";
                vm.CoName = !String.IsNullOrEmpty(CustomerId) ? _context.Cstm10.Find(CustomerId)?.Coname : "";
                vm.Memo = LastOut != null ? LastOut?.Memo : "";
                vm.OutDate = DateTime.Today.AddDays(1).ToString("yyyyMMdd");
                vm.PayMonth = DateTime.Today.AddDays(25).ToString("yyyMM");
                vm.KeyInDate = DateTime.Today.ToString("yyyyMMdd");
                vm.Ntus = "NTD";
                vm.Userid = HttpContext.Session.GetString("UserAc");
                vm.PeNo = LastOut != null ? LastOut?.PeNo : (CSTM != null ? CSTM.PeNo : "");
                vm.DriveNo = LastOut != null ? LastOut?.DriveNo : (CSTM != null ? CSTM.DriveNo : "");
                vm.Discount = LastOut != null ? LastOut?.Discount : (CSTM != null ? CSTM.Discount : 0);
                vm.TallyState = "2";

                // 所有分類
                vm.StockTypeList = await _context.Stock_Type.Where(x => x.TYPE_ISOPEN).OrderBy(x => x.TYPE_ORDER).Select(x => x.TYPE_NAME).ToListAsync();

                sw.Start();

                var allStock = _context.Stock10.Where(x => vm.StockTypeList.Contains(x.Type) && (bool)x.IsOpen).OrderBy(x => x.PartNo).Select(x => x.PartNo).ToList();

                var AllPrice = !String.IsNullOrEmpty(CustomerId) ? GetStockPrice(CustomerId, allStock) : null;

                sw.Stop();

                // 所有產品
                vm.StockList = await _context.Stock10.Where(x => vm.StockTypeList.Contains(x.Type) && (bool)x.IsOpen).OrderBy(x => x.PartNo).Select(x => new StockList
                {
                    Type = x.Type ?? "",
                    IsSales = false,
                    IsStats = LastOut20.Contains(x.PartNo ?? ""),
                    PartNo = x.PartNo ?? "",
                    Spec = x.Spec ?? "",
                    Price = AllPrice != null ? AllPrice[x.PartNo] : 0,
                    Qty = null,
                    IQty = x.InitQty1,
                }).ToListAsync();

                return View(vm);
            }
            catch (Exception e)
            {
                throw;
            }
        }

        [HttpGet]
        // GET: Sales
        //public async Task<IActionResult> OrderEdit(string id, string SAREA, string SCLIENT)
        public async Task<IActionResult> OrderEdit_old(string OutNo, string Area, string CustomerId)
        {
            try
            {
                #region Again

                var vm = new OrderEditViewModel();

                OutNo = OutNo ?? "";
                Area = Area ?? "";
                CustomerId = CustomerId ?? "";

                // 新增
                if (String.IsNullOrEmpty(OutNo))
                {
                    vm.OutNo = OutNo;

                    var cstm = _context.Cstm10.Find(CustomerId);

                    vm.CoName = cstm != null ? cstm.Coname : "";

                    // 抓取上一筆訂單資料
                    var lastOut = !String.IsNullOrEmpty(CustomerId) ? _context.Out10.Where(x => x.CoNo == CustomerId && x.OutType == "1").OrderByDescending(x => x.OutNo).FirstOrDefault()
                        : new Out10();

                    vm.OrderState = "1";

                    vm.OrderTime = "";

                    vm.Area = Area ?? "";

                    vm.CustomerId = CustomerId ?? "";

                    // 開啟顯示的所有產品分類
                    var opentypes = _context.Stock_Type.Where(x => x.TYPE_ISOPEN == true).OrderBy(x => x.TYPE_ORDER).Select(x => x.TYPE_NAME).ToList();
                    vm.StockTitleList = opentypes.Select(x => new StockTitle { Name = x }).ToList();

                    // 所有上方分類裡的產品
                    vm.StockList = _context.Stock10.Where(x => x.IsOpen == true && opentypes.Contains(x.Type)).Select(x => new StockInfo
                    {
                        IsPromise = false,
                        PartNo = x.PartNo,
                        Spec = x.Spec,
                        StockType = x.Type,
                        Price = x.Price1 ?? 0,
                        OriPrice = 0,
                        //PackQty = 0
                    }).ToList();

                    // 出貨產品列表 顯示上次訂單的資料                    
                    vm.OutDetail = lastOut.OutNo != null ? _context.Out20.Where(x => x.OutNo == lastOut.OutNo).Select(x => new OutOrderDetail
                    {
                        OutDetailNo = x.OutNo,
                        PartNo = x.PartNo,
                        IsPromise = false,
                        Stats = false,
                        //Name = _context.Stock10.Find(x.PartNo) != null ? _context.Stock10.Find(x.PartNo).Spec.ToString() : "",
                        Name = _context.Stock10.Where(x => x.PartNo == x.PartNo).FirstOrDefault() != null ? _context.Stock10.Where(x => x.PartNo == x.PartNo).FirstOrDefault().Spec.ToString() : "",
                        Qty = x.Qty,
                        Memo = x.Memo,
                        Price = x.Price ?? 0
                    }).ToList() : new List<OutOrderDetail>();

                    // 如果有促銷
                    var stock20 = _context.Stock20.Where(x => x.CoNo == CustomerId).OrderByDescending(x => x.SpNo).FirstOrDefault();
                    var today = DateTime.Today.ToString("yyyyMMdd");

                    // 更改所有產品合併上次訂單的狀態
                    var detailPartNo = vm.OutDetail.Select(x => x.PartNo).ToList();
                    // 合併 加入約定、促銷、設定價格
                    foreach (var stock in vm.StockList)
                    {
                        // 價格 約定價格 > 促銷價 > 設定價格

                        // 先加入促銷和設定價格
                        var stockdetail = vm.StockList.Where(x => x.PartNo == stock.PartNo).FirstOrDefault();
                        // 先加入設定價格
                        var pricetype = cstm != null ? cstm.PriceType : "";
                        var aaa = _context.PRICE_TYPE.ToList();

                        var stockcol = _context.PRICE_TYPE.Where(x => x.PT_VALUE == pricetype).FirstOrDefault();
                        double? pp = 0;
                        if (stockcol != null)
                        {
                            switch (stockcol.PT_STOCKCOL)
                            {
                                case "Price1":
                                    pp = _context.Stock10.Find(stock.PartNo).Price1;
                                    break;
                                case "Price2":
                                    pp = _context.Stock10.Find(stock.PartNo).Price2;
                                    break;
                                case "Price3":
                                    pp = _context.Stock10.Find(stock.PartNo).Price3;
                                    break;
                                case "DefaultPrice1":
                                    pp = _context.Stock10.Find(stock.PartNo).InitCost1;
                                    break;
                                case "DefaultPrice2":
                                    pp = _context.Stock10.Find(stock.PartNo).InitCost2;
                                    break;
                                case "DefaultPrice3":
                                    pp = _context.Stock10.Find(stock.PartNo).InitCost3;
                                    break;
                                case "DefaultPrice4":
                                    pp = _context.Stock10.Find(stock.PartNo).InitCost4;
                                    break;
                                case "DefaultPrice5":
                                    pp = _context.Stock10.Find(stock.PartNo).InitCost5;
                                    break;
                                default:
                                    pp = 0;
                                    break;
                            }
                        }
                        else
                        {
                            pp = 0;
                        }
                        stockdetail.Price = Convert.ToDouble(pp);
                        stockdetail.OriPrice = Convert.ToDouble(pp);

                        // 加入歷史資料
                        if (detailPartNo.Contains(stock.PartNo))
                        {
                            var outdetail = vm.OutDetail.Where(x => x.PartNo == stock.PartNo).FirstOrDefault();
                            if (stockdetail != null)
                            {
                                //stockdetail.PackQty = outdetail.Qty;
                                stockdetail.IsSalesStatus = true;
                                stockdetail.StPs = outdetail.Memo;
                                stockdetail.Price = outdetail.Price;
                                stockdetail.OriPrice = outdetail.OPrice;
                            }
                        }

                        // 加入促銷和約定
                        if (!String.IsNullOrEmpty(CustomerId))
                        {
                            if (stock20 != null)
                            {
                                var checksales = (int.Parse(stock20.Bdate) <= int.Parse(DateTime.Now.ToString("yyyyMMdd")) && int.Parse(stock20.Edate) >= int.Parse(DateTime.Now.ToString("yyyyMMdd")));
                                // 如果促銷有此產品
                                if (checksales)
                                {
                                    var stock21 = _context.Stock21.Where(x => x.SpNo == stock20.SpNo && x.PartNo == stock.PartNo).FirstOrDefault();
                                    stockdetail.Price = stock21 != null ? Convert.ToDouble(stock21.Newprice) : stockdetail.Price;
                                    stockdetail.OriPrice = stock21 != null ? Convert.ToDouble(stock21.Newprice) : stockdetail.Price;
                                }
                            }

                            // 查找約定
                            var promisedata = _context.PROMISE_PRICE.Where(x => x.PART_NO == stock.PartNo && x.CO_NO == CustomerId).OrderByDescending(x => x.PP_NO).FirstOrDefault();
                            stockdetail.Price = promisedata != null ? Convert.ToDouble(promisedata.PRICE) : stockdetail.Price;
                            stockdetail.IsPromise = promisedata != null;
                        }
                        // 如果抓不到客戶

                    }

                    vm.Memo = lastOut.Memo;
                    vm.Business = lastOut.PeNo;
                    vm.Driver = lastOut.DriveNo;

                    // 畫面資料
                    ViewBag.FUNNAME = "新增訂單";
                }
                // 編輯
                else
                {
                    // 拿出出貨單
                    var Out10 = _context.Out10.Find(OutNo);
                    var cstm = _context.Cstm10.Find(CustomerId);
                    if (Out10 == null) return NotFound();

                    vm.OutNo = OutNo;
                    vm.CoName = cstm != null ? cstm.Coname : "";
                    vm.OrderState = Out10.OutType;
                    vm.OrderTime = Out10.OutDate;
                    vm.Area = Area;
                    vm.CustomerId = CustomerId;
                    vm.Memo = Out10.Memo;
                    vm.Business = Out10.PeNo;
                    vm.Driver = Out10.DriveNo;

                    // 開啟顯示的所有產品分類
                    var opentypes = _context.Stock_Type.Where(x => x.TYPE_ISOPEN == true).OrderBy(x => x.TYPE_ORDER).Select(x => x.TYPE_NAME).ToList();
                    vm.StockTitleList = opentypes.Select(x => new StockTitle { Name = x }).ToList();

                    // 所有上方分類裡的產品
                    vm.StockList = _context.Stock10.Where(x => x.IsOpen == true && opentypes.Contains(x.Type)).Select(x => new StockInfo
                    {
                        IsPromise = false,
                        PartNo = x.PartNo,
                        Spec = x.Spec,
                        StockType = x.Type,
                        Price = x.Price1 ?? 0,
                        OriPrice = 0,

                    }).ToList();

                    // 出貨產品列表 顯示上次訂單的資料                    
                    vm.OutDetail = Out10.OutNo != null ? _context.Out20.Where(x => x.OutNo == Out10.OutNo).Select(x => new OutOrderDetail
                    {
                        OutDetailNo = x.OutNo,
                        PartNo = x.PartNo,
                        IsPromise = !String.IsNullOrEmpty(x.IsPromise),
                        Stats = false,
                        //Name = _context.Stock10.Find(x.PartNo) != null ? _context.Stock10.Find(x.PartNo).Spec.ToString() : "",
                        Name = _context.Stock10.Where(x => x.PartNo == x.PartNo).FirstOrDefault() != null ? _context.Stock10.Where(x => x.PartNo == x.PartNo).FirstOrDefault().Spec.ToString() : "",
                        Qty = x.Qty,
                        Memo = x.Memo,
                        Price = x.Price ?? 0
                    }).ToList() : new List<OutOrderDetail>();

                    // 如果有促銷
                    //var stock20 = _context.Stock20.Where(x => x.CoNo == CustomerId).OrderByDescending(x => x.SpNo).FirstOrDefault();
                    //var today = DateTime.Today.ToString("yyyyMMdd");

                    // 更改所有產品合併上次訂單的狀態
                    var detailPartNo = vm.OutDetail.Select(x => x.PartNo).ToList();
                    // 合併 加入約定、促銷、設定價格
                    foreach (var stock in vm.StockList)
                    {
                        // 價格 約定價格 > 促銷價 > 設定價格

                        // 先加入促銷和設定價格
                        var stockdetail = vm.StockList.Where(x => x.PartNo == stock.PartNo).FirstOrDefault();
                        // 先加入設定價格
                        var pricetype = cstm != null ? cstm.PriceType : "";
                        var aaa = _context.PRICE_TYPE.ToList();

                        var stockcol = _context.PRICE_TYPE.Where(x => x.PT_VALUE == pricetype).FirstOrDefault();
                        double? pp = 0;
                        if (stockcol != null)
                        {
                            switch (stockcol.PT_STOCKCOL)
                            {
                                case "Price1":
                                    pp = _context.Stock10.Find(stock.PartNo).Price1;
                                    break;
                                case "Price2":
                                    pp = _context.Stock10.Find(stock.PartNo).Price2;
                                    break;
                                case "Price3":
                                    pp = _context.Stock10.Find(stock.PartNo).Price3;
                                    break;
                                case "DefaultPrice1":
                                    pp = _context.Stock10.Find(stock.PartNo).InitCost1;
                                    break;
                                case "DefaultPrice2":
                                    pp = _context.Stock10.Find(stock.PartNo).InitCost2;
                                    break;
                                case "DefaultPrice3":
                                    pp = _context.Stock10.Find(stock.PartNo).InitCost3;
                                    break;
                                case "DefaultPrice4":
                                    pp = _context.Stock10.Find(stock.PartNo).InitCost4;
                                    break;
                                case "DefaultPrice5":
                                    pp = _context.Stock10.Find(stock.PartNo).InitCost5;
                                    break;
                                default:
                                    pp = 0;
                                    break;
                            }
                        }
                        else
                        {
                            pp = 0;
                        }
                        stockdetail.Price = Convert.ToDouble(pp);
                        stockdetail.OriPrice = Convert.ToDouble(pp);

                        // 加入促銷和約定
                        if (!String.IsNullOrEmpty(CustomerId))
                        {
                            var stock20 = _context.Stock20.Where(x => x.CoNo == CustomerId).OrderByDescending(x => x.SpNo).FirstOrDefault();

                            if (stock20 != null)
                            {
                                var checksales = (int.Parse(stock20.Bdate) <= int.Parse(DateTime.Now.ToString("yyyyMMdd")) && int.Parse(stock20.Edate) >= int.Parse(DateTime.Now.ToString("yyyyMMdd")));
                                // 如果促銷有此產品
                                if (checksales)
                                {
                                    var stock21 = _context.Stock21.Where(x => x.SpNo == stock20.SpNo && x.PartNo == stock.PartNo).FirstOrDefault();
                                    stockdetail.Price = stock21 != null ? Convert.ToDouble(stock21.Newprice) : stockdetail.Price;
                                    stockdetail.OriPrice = stock21 != null ? Convert.ToDouble(stock21.Newprice) : stockdetail.Price;
                                }
                            }
                            // 查找約定
                            var promisedata = _context.PROMISE_PRICE.Where(x => x.PART_NO == stock.PartNo && x.CO_NO == CustomerId).OrderByDescending(x => x.PP_NO).FirstOrDefault();
                            stockdetail.Price = promisedata != null ? Convert.ToDouble(promisedata.PRICE) : stockdetail.Price;
                            stockdetail.IsPromise = promisedata != null;
                        }
                        // 如果抓不到客戶


                        // 加入歷史資料
                        if (detailPartNo.Contains(stock.PartNo))
                        {
                            var outdetail = vm.OutDetail.Where(x => x.PartNo == stock.PartNo).FirstOrDefault();
                            if (stockdetail != null)
                            {
                                stockdetail.PackQty = outdetail.Qty;
                                stockdetail.IsSalesStatus = true;
                                stockdetail.StPs = outdetail.Memo;
                                stockdetail.Price = outdetail.Price;
                                stockdetail.OriPrice = outdetail.OPrice;
                                stockdetail.IsPromise = outdetail.IsPromise;
                            }
                        }
                    }

                    ViewBag.FUNNAME = "修改訂單";
                }

                // selectlist
                ViewBag.CLient = !String.IsNullOrEmpty(Area) ? new SelectList(_context.Cstm10.Where(x => x.AreaNo == Area).ToList(), "CoNo", "Company", vm.CustomerId)
                    : new SelectList(_context.Cstm10.ToList(), "CoNo", "Company", vm.CustomerId);

                return View(vm);

                #endregion Again

                //#region new


                //var vm = new OrderEditViewModel();

                //vm.OutNo = OutNo;

                //if (!string.IsNullOrEmpty(CustomerId))
                //{
                //    vm.CoName = _context.Cstm10.Where(x => x.CoNo == CustomerId).FirstOrDefault().Coname;
                //}



                //vm.Area = Area;

                //vm.CustomerId = CustomerId;
                //ViewBag.Client = new SelectList(_context.Cstm10.Where(x => x.AreaNo == Area).ToList(), "CoNo", "Company", vm.CustomerId);

                ////取得公司資料
                ////if (!string.IsNullOrEmpty(CustomerId))
                ////{
                ////    var companyData = await _context.Cstm10.Where(x => x.CoNo == CustomerId).FirstOrDefaultAsync();
                ////}

                //////取得業務人員資料
                ////if (!string.IsNullOrEmpty(companyData.PeNo))
                ////{
                ////    vm.Business = _context.Pepo10
                ////        .Where(x => x.PeNo == companyData.PeNo).FirstOrDefault().Name;
                ////}

                //////取得司機人員資料
                ////if (!string.IsNullOrEmpty(companyData.DriveNo))
                ////{
                ////    vm.Driver = _context.Pepo10
                ////    .Where(x => x.PeNo == companyData.DriveNo).FirstOrDefault().Name;
                ////}

                ////先取分類列表
                ////var title = _context.Stock10.Where(x => x.IsOpen == true).Select(x => new StockTitle
                ////{
                ////    Name = x.Type
                ////}).Distinct().OrderBy(x => x.Name).ToList();
                //var title = _context.Stock_Type.Where(x => x.TYPE_ISOPEN == true).OrderBy(x => x.TYPE_ORDER).Select(x=>new StockTitle { Name = x.TYPE_NAME }).ToList();

                //vm.StockTitleList = title;

                ////取出出貨單主表
                //var outOrderMain = _context.Out10.Where(x => x.OutNo == vm.OutNo).FirstOrDefault();

                //if (outOrderMain != null)
                //{
                //    vm.CustomerId = outOrderMain.CoNo;
                //    vm.CoName = _context.Cstm10.Where(x => x.CoNo == outOrderMain.CoNo).FirstOrDefault().Coname;
                //}

                ////TODO 訂單紀錄(單筆)By Alex
                //#region 訂單紀錄(單筆)

                //var outOrderDetailData = _context.Out20
                //    .Where(x => x.OutNo == (vm != null ? vm.OutNo : string.Empty)).ToList();

                //vm.OutDetail = new List<OutOrderDetail>();

                //foreach (var item in outOrderDetailData)
                //{
                //    var OutDetail = new OutOrderDetail();

                //    OutDetail.OutDetailNo = item.OutNo;
                //    OutDetail.PartNo = item.PartNo;
                //    OutDetail.Sale = false;
                //    OutDetail.Stats = false;
                //    //品名
                //    OutDetail.Name = _context.Stock10.Where(x => x.PartNo == item.PartNo).Select(x => x.Spec).ToString();
                //    OutDetail.Qty = item.Qty;
                //    OutDetail.Memo = item.Memo;

                //    vm.OutDetail.Add(OutDetail);
                //}

                //#endregion 訂單紀錄(單筆)

                //#region 新增/修改

                //var promotionData = new Stock20();
                //var checkTime = false;

                //if (string.IsNullOrEmpty(vm.OutNo)) //新增
                //{
                //    ViewBag.FUNNAME = "新增訂單";

                //    vm.OrderTime = DateTime.Now.ToString("yyyy-MM-dd");

                //    //檢查是否有促銷資料
                //    promotionData = _context.Stock20
                //            .Where(x => x.CoNo == CustomerId)
                //            .OrderByDescending(x => x.SpNo).FirstOrDefault();

                //    if (promotionData != null)
                //    {
                //        //如果有在比對日期是否在期限內
                //        checkTime = (int.Parse(promotionData.Bdate) <= int.Parse(DateTime.Now.ToString("yyyyMMdd"))
                //            && int.Parse(promotionData.Edate) >= int.Parse(DateTime.Now.ToString("yyyyMMdd")));
                //    }
                //}
                //else //修改
                //{
                //    ViewBag.FUNNAME = "修改訂單";

                //    vm.OrderTime = DateTime.ParseExact(outOrderMain.OutDate, "yyyyMMdd", null).ToString("yyyy-MM-dd");
                //    vm.Memo = outOrderMain.Memo;

                //    //檢查是否有促銷資料
                //    if (outOrderMain.Promotion_No != null)
                //    {
                //        checkTime = true;
                //    }
                //}

                //#endregion

                ////TODO 促銷 販售&狀態待確認 By Alex
                //#region 寫入資料

                ////先找出所有訂單
                ////var allOut10Data = await _context.Out10.Where(x => x.CoNo == vm.CustomerId).Select(x => x.OutNo).ToListAsync();

                //// 找出所有此筆訂單的產品
                //var allData = (from out10 in _context.Out10
                //               join out20 in _context.Out20 on out10.OutNo equals out20.OutNo
                //               where out10.CoNo.Equals(vm.CustomerId)
                //               select new { out20.PartNo }).Distinct().ToList();

                //Dictionary<int, string> dict = new Dictionary<int, string>();

                //// 取出所有的產品ID
                //if (allData.Count() > 0)
                //{
                //    for (int i = 0; i < allData.Count(); i++)
                //    {
                //        dict.Add(i, allData[i].PartNo);
                //    }
                //}

                //vm.StockList = new List<StockInfo>();

                //foreach (var titleItem in title)
                //{
                //    // 取出此類別的所有產品
                //    var stockData = await _context.Stock10
                //        .Where(x => x.Type == titleItem.Name)
                //        .Select(x => new StockInfo
                //        {
                //            PartNo = x.PartNo,
                //            Spec = x.Spec,
                //            StockType = x.Type
                //        }).OrderBy(x => x.PartNo).ToListAsync();


                //    foreach (var stock in stockData)
                //    {
                //        // 如果曾經買過
                //        if (dict.Where(x => x.Value == stock.PartNo).Any())
                //        {
                //            stock.IsSalesStatus = true;
                //        }

                //        //塞入促銷單對應資料
                //        if (outOrderMain != null)
                //        {
                //            if (checkTime)
                //            {
                //                var promotionDataDetail = _context.Stock21.Where(x => x.SpNo == outOrderMain.Promotion_No).ToList();

                //                if (promotionDataDetail != null)
                //                {
                //                    foreach (var promotionDataDetailItem in promotionDataDetail)
                //                    {
                //                        if (promotionDataDetailItem.PartNo == stock.PartNo)
                //                        {
                //                            stock.IsPromotion = true;
                //                        }
                //                    }
                //                }
                //            }
                //        }
                //        else
                //        {
                //            if (checkTime)
                //            {
                //                var promotionDataDetail = _context.Stock21.Where(x => x.SpNo == outOrderMain.Promotion_No).ToList();

                //                if (promotionDataDetail != null)
                //                {
                //                    foreach (var promotionDataDetailItem in promotionDataDetail)
                //                    {
                //                        if (promotionDataDetailItem.PartNo == stock.PartNo)
                //                        {
                //                            stock.IsPromotion = true;
                //                        }
                //                    }
                //                }
                //            }
                //        }

                //        //20221224 item2.PackQty = null;
                //        if (stock.PackQty == null)//數量
                //            stock.PackQty = null;
                //        else
                //            stock.PackQty = null;

                //        stock.StPs = "";

                //        if (outOrderDetailData != null)
                //        {
                //            if (vm.OutDetail != null)
                //            {
                //                foreach (var outDetailItem in vm.OutDetail.Where(x => x.PartNo == stock.PartNo))
                //                {
                //                    stock.PackQty = outDetailItem.Qty;
                //                    stock.StPs = outDetailItem.Memo;
                //                }
                //            }
                //        }
                //    }

                //    vm.StockList.AddRange(stockData);
                //}

                //if (vm.StockList != null)
                //{
                //    vm.StockList = vm.StockList
                //        .OrderByDescending(x => x.IsPromotion)
                //        .OrderByDescending(x => x.IsSalesStatus)
                //        .ToList();
                //}

                //#endregion 寫入資料

                //vm.StockTitleList = title;

                //return View(vm);

                //#endregion new

            }
            catch (Exception)
            {

                throw;
            }
        }

        [HttpGet]
        public async Task<IActionResult> OrderEdit(string OutNO, string Area, string CustomerId)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            try
            {
                // 建立模型
                var vm = new OrderEditViewModel();

                OutNO = OutNO ?? "";
                Area = Area ?? "";
                CustomerId = CustomerId ?? "";

                // 找出客戶
                var CSTM = _context.Cstm10.Find(CustomerId);

                // 找出訂單資料
                var OUTDATA = OutNO != null ? _context.Out10.Find(OutNO) : null;

                // 上筆歷史資料
                var LAST_OUT = !String.IsNullOrEmpty(OutNO) ? await _context.Out10.Where(x => x.OutType == "1" && x.CoNo == CustomerId && String.Compare(x.OutNo, OutNO) < 0).OrderByDescending(x => x.OutNo).FirstOrDefaultAsync()
                                    : await _context.Out10.Where(x => x.OutType == "1" && x.CoNo == CustomerId).OrderByDescending(x => x.OutNo).FirstOrDefaultAsync();

                // 上筆訂單的產品
                var LAST_OUTLIST = String.IsNullOrEmpty(OutNO) ?
                    (!String.IsNullOrEmpty(CustomerId) ? _context.Out10.Where(x => x.CoNo == CustomerId && x.OutType == "1") : null) :
                    _context.Out10.Where(x => x.CoNo == CustomerId && String.Compare(x.OutNo, OutNO) <= 0 && x.OutNo != OutNO && x.OutType == "1");

                var LAST_STOCK = LAST_OUTLIST != null ? _context.Out20.Join(LAST_OUTLIST, x => x.OutNo, y => y.OutNo, (x, y) => new { PartNo = x.PartNo, Qty = x.Qty })
                    .Where(x => x.Qty > 0).Select(x => x.PartNo).Distinct().ToList() : new List<string>();

                // 所有的產品分類
                var Types = await _context.Stock_Type.Where(x => x.TYPE_ISOPEN).OrderBy(x => x.TYPE_ORDER).Select(x => x.TYPE_NAME).ToListAsync();

                // 所有分類裡的產品 ( 顯示的主體 ) 
                var ALLSTOCKLIST = await _context.Stock10.Where(x => Types.Contains(x.Type) && (bool)x.IsOpen).ToListAsync();

                // 所有產品的價格
                var StockList_PRICE = _ajaxController.GetPartPrice(CustomerId, ALLSTOCKLIST.Select(x => x.PartNo).ToList());

                // 帶入資料 - 產品資訊
                vm.StockList = await ALLSTOCKLIST.Select(x => new StockInfo
                {
                    //IsPromise
                    PartNo = x.PartNo,
                    Spec = x.Spec,
                    //PackQty
                    IsSalesStatus = LAST_STOCK.Contains(x.PartNo),
                    StPs = "",
                    StockType = x.Type,
                    Price = !String.IsNullOrEmpty(CustomerId) ? StockList_PRICE[x.PartNo] : 0,
                    OriPrice = !String.IsNullOrEmpty(CustomerId) ? StockList_PRICE[x.PartNo] : 0,
                    IsPromotion = false,
                    stQty = x.StQty,
                }).ToListAsync();

                // 帶入資料 - 產品分類
                vm.StockTitleList = await Types.Select(x => new StockTitle { Name = x }).ToListAsync();

                // 帶入資料
                vm.OutNo = OutNO;
                vm.CoName = CSTM?.CoNo;
                vm.OrderState = OUTDATA != null ? OUTDATA.OutType : "1"; // 預設為1:出貨
                vm.OrderTime = OUTDATA != null ? OUTDATA.OutDate : "";
                vm.Area = Area ?? "";
                vm.CustomerId = CustomerId ?? "";
                vm.Memo = OUTDATA != null ? OUTDATA.Memo : (LAST_OUT != null ? LAST_OUT.Memo : "");
                vm.Business = OUTDATA != null ? OUTDATA.PeNo : (LAST_OUT != null ? LAST_OUT.PeNo : CSTM != null ? CSTM.PeNo : "");
                vm.Driver = OUTDATA != null ? OUTDATA.DriveNo : (LAST_OUT != null ? LAST_OUT.DriveNo : CSTM != null ? CSTM.DriveNo : "");
                vm.IsCopy = "";

                // 修改 - 帶入出貨單的詳細資料
                if (OUTDATA != null)
                {
                    var OUTSTOCK = await _context.Out20.Where(x => x.OutNo == OUTDATA.OutNo).ToListAsync();

                    foreach (var stock in vm.StockList)
                    {
                        var stockdetail = OUTSTOCK.Where(x => x.PartNo == stock.PartNo).FirstOrDefault();
                        stock.PackQty = stockdetail != null ? stockdetail?.Qty : stock.PackQty;
                        stock.IsPromise = stockdetail != null ? !String.IsNullOrEmpty(stockdetail?.IsPromise) : false;
                        stock.Price = stockdetail != null ? Convert.ToDouble(stockdetail?.Price) : stock.Price;
                        stock.StPs = stockdetail != null ? stockdetail?.Memo : stock.StPs;
                    }
                }
                // 新增  - 帶入出貨單的詳細資料
                else
                {
                    var LASTOUT = _context.Out10.Where(x => x.CoNo == CustomerId && x.OutType == "1").Select(x => x.OutNo).ToList();
                    var OUTSTOCK = _context.Out20.Where(x => LASTOUT.Contains(x.OutNo)).ToList();

                    foreach (var stock in vm.StockList)
                    {
                        var stockdetail = OUTSTOCK.Where(x => x.PartNo == stock.PartNo).OrderByDescending(x => x.OutNo).FirstOrDefault();
                        stock.PackQty = null;
                        stock.IsPromise = stockdetail != null ? !String.IsNullOrEmpty(stockdetail?.IsPromise) : false;
                        stock.StPs = stockdetail != null ? stockdetail?.Memo : stock.StPs;
                    }

                }

                // 畫面資料
                ViewBag.CLient = !String.IsNullOrEmpty(Area) ? new SelectList(_context.Cstm10.Where(x => x.AreaNo == Area).ToList(), "CoNo", "Coname", vm.CustomerId)
                                : new SelectList(_context.Cstm10.ToList(), "CoNo", "Coname", vm.CustomerId);

                return View(vm);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OrderEdit(OrderEditViewModel vm, List<string> SALE, List<string> PART_NO, List<string> SPEC, List<string> PORDPRICE, List<string> PORDNUM, List<string> PS)
        {
            string logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Log");
            var logger = new Logger(logDirectory);

            try
            {
                if (ModelState.IsValid)
                {
                    var INSERTDATA = new Out10();
                    var CSTM = _context.Cstm10.Find(vm.CustomerId ?? "");

                    // 重新計算資料
                    #region 重新計算資料

                    var aa = _context.Cstm10.Find(vm.CustomerId)?.Discount;
                    var bb = Convert.ToDouble(aa);

                    // 客戶折扣
                    double DISCOUNT = !String.IsNullOrEmpty(vm.CustomerId) ? (double)(_context.Cstm10.Find(vm.CustomerId)?.Discount ?? 0) : 0;
                    double CASHDIS = 0;
                    // 未收金額
                    double NOTGET = 0;
                    // 免稅總計
                    double TOTAL1 = 0;
                    // 應稅總計
                    double TOTAL0 = 0;
                    // 總計
                    double TOTAL = 0;
                    // 公斤
                    double KG = 0;
                    // 已收
                    double YESGET = 0; // 預設0
                    // 折讓
                    double SUBTOT = 0; // 預設0

                    #endregion 重新計算資料

                    if (String.IsNullOrEmpty(vm.OutNo))
                    {
                        // 新增
                        #region 新增

                        // 建立單號
                        string newOutNO = string.Empty;

                        // 找出這個月最新的單號
                        var cond = DateTime.Now.ToString("yyyMM");

                        //20240628 Ethan
                        //var lastOutNo = _context.Out10.Where(x => x.OutNo.Contains(cond)).OrderByDescending(x => x.OutNo).FirstOrDefault()?.OutNo;
                        var lastOutNo = _context.Out10.Where(x => x.OutNo.Contains(cond) && x.OutType == "1").OrderByDescending(x => x.OutNo).FirstOrDefault()?.OutNo;
                        //20240628 Ethan

                        //20240702 Ethan 修正單號產生
                        // 如果有最新單號
                        //newOutNO = !String.IsNullOrEmpty(lastOutNo) ? (int.Parse(lastOutNo) + 1).ToString() : cond + "5001";

                        if (!string.IsNullOrEmpty(lastOutNo))
                        {
                            newOutNO = (int.Parse(lastOutNo) + 1).ToString();
                            var sb = new StringBuilder(newOutNO);

                            if (sb[6] != '5')
                                sb[6] = '5';

                            newOutNO = sb.ToString();
                        }
                        else
                        {
                            newOutNO = cond + "5001";
                        }
                        //20240702 Ethan

                        // 計算帳款月份 依客戶結帳日、出貨日期為準
                        var PayMonth = "";
                        var dlien = CSTM.Dlien ?? "";
                        var outDate = DateTime.Now.AddDays(1); // 預設+1
                        var outDay = outDate.Day;
                        var outYearMonth = outDate.Year.ToString() + outDate.Month.ToString().PadLeft(2, '0');

                        if (!String.IsNullOrEmpty(dlien) && dlien != "月底")
                        {
                            if (Convert.ToInt32(dlien) >= outDay)
                            {
                                PayMonth = outYearMonth;
                            }
                            else
                            {
                                PayMonth = DateTime.ParseExact(outYearMonth, "yyyyMM", null).AddMonths(1).ToString("yyyyMM");
                            }
                        }
                        else
                        {
                            PayMonth = outYearMonth;
                        }


                        Out10 insertData = new Out10
                        {
                            OutNo = newOutNO,
                            OutType = "1", // 預設 1:出貨
                            OutDate = DateTime.Now.AddDays(1).ToString("yyyyMMdd"), // 出貨日期為建檔+1天
                            Paymonth = PayMonth, // 客戶結帳日前為當月，後為下月
                            Ntus = "NTD",
                            CoNo = vm.CustomerId ?? "",
                            Memo = vm.Memo ?? "",
                            Total1 = TOTAL1,
                            Userid = HttpContext.Session.GetString("UserAc") ?? "",
                            StockPass = null,
                            YnClose = null,
                            PeNo = vm.Business ?? (CSTM != null ? CSTM.PeNo : ""),
                            KeyinDate = DateTime.Now.ToString("yyyyMMddHHmm"),
                            YesGet = YESGET, // 預設為0
                            CashDis = CASHDIS,
                            SubTot = SUBTOT, // 預設為0
                            NotGet = NOTGET,
                            Total0 = TOTAL0,
                            Total = TOTAL,
                            DriveNo = vm.Driver ?? (CSTM != null ? CSTM.DriveNo : ""),
                            Discount = 0,
                            Tax = 0, // 預設
                            Kg = KG,
                            Promotion_No = null,
                            TallyState = "2", // 預設為未理貨
                            EDITID = HttpContext.Session.GetString("UserAc") ?? "",
                        };

                        var NEWOUT10DATA = _context.Out10.Add(insertData);
                        INSERTDATA = NEWOUT10DATA.Entity;
                        await _context.SaveChangesAsync();

                        vm.OutNo = newOutNO;

                        var properties = typeof(Out10).GetProperties();
                        var logMsg = string.Join(",", properties.Select(x => $"{x.Name}={x.GetValue(insertData)}"));
                        logger.WriteLog(vm.CustomerId, logMsg);
                        #endregion 新增
                    }
                    else
                    {
                        // 編輯
                        #region 編輯

                        INSERTDATA = _context.Out10.Find(vm.OutNo);

                        INSERTDATA.CoNo = vm.CustomerId;
                        INSERTDATA.Memo = vm.Memo;
                        INSERTDATA.EDITID = HttpContext.Session.GetString("UserAc");
                        INSERTDATA.OutType = "1"; // 固定編輯後即為出貨

                        YESGET = (double)INSERTDATA.YesGet;
                        SUBTOT = (double)INSERTDATA.SubTot;

                        // 刪除明細
                        _context.Out20.RemoveRange(_context.Out20.Where(x => x.OutNo == vm.OutNo).ToList());
                        await _context.SaveChangesAsync();

                        var properties = typeof(Out10).GetProperties();
                        var logMsg = string.Join(",", properties.Select(x => $"{x.Name}={x.GetValue(INSERTDATA)}"));
                        logger.WriteLog(vm.CustomerId, logMsg);
                        #endregion 編輯
                    }

                    // 加入明細資料
                    #region 加入明細資料

                    int i = 0;
                    int SERNO = 1;

                    // 取得所有歷史出貨明細資料
                    var outlist = _context.Out10.Where(x => x.CoNo == vm.CustomerId && x.OutType == "1").Select(x => x.OutNo).ToList();
                    var out20Detail = _context.Out20.Where(x => outlist.Contains(x.OutNo)).ToList();

                    foreach (var partNO in PART_NO)
                    {
                        if (Convert.ToInt32(PORDNUM[i]) > 0)
                        {
                            // 取得產品資料
                            var STOCKDATA = _context.Stock10.Find(partNO);
                            // 取得上筆明細資料
                            var history_stock = out20Detail.Where(x => x.PartNo == partNO).OrderByDescending(x => x.OutNo).FirstOrDefault();
                            // 數量
                            double STOCK_QTY = Convert.ToDouble(PORDNUM[i]);
                            // 價格
                            double STOCK_PRICE = Convert.ToDouble(PORDPRICE[i]);
                            // 商品折扣
                            double STOCK_DIS = history_stock != null && history_stock.Discount != null ? (double)history_stock.Discount : (STOCKDATA != null ? Convert.ToDouble(STOCKDATA.LastDiscount) : 0);
                            // 總計
                            double STOCK_AMOUNT = Math.Round((STOCK_PRICE * STOCK_QTY) * (100 - STOCK_DIS) / 100, 2);
                            // 建議售價
                            double? STOCK_SP = 0;
                            // 牌價
                            double? STOCK_PP = 0;

                            //找出牌價和建議售價
                            #region 找出牌價和建議售價

                            var CSTM_PTYPE = CSTM != null ? CSTM.PriceType : "";
                            var STOCK_COL = _context.PRICE_TYPE.Where(x => x.PT_VALUE == CSTM_PTYPE).FirstOrDefault()?.PT_STOCKCOL;

                            switch (STOCK_COL)
                            {
                                case "Price1":
                                    STOCK_PP = STOCKDATA?.Price1;
                                    STOCK_SP = STOCKDATA?.SPrice1;
                                    break;
                                case "Price2":
                                    STOCK_PP = STOCKDATA?.Price2;
                                    STOCK_SP = STOCKDATA?.SPrice2;
                                    break;
                                case "Price3":
                                    STOCK_PP = STOCKDATA?.Price3;
                                    STOCK_SP = STOCKDATA?.SPrice3;
                                    break;
                                case "DefaultPrice1":
                                    STOCK_PP = STOCKDATA?.DefaultPrice1;
                                    STOCK_SP = STOCKDATA?.SPrice4;
                                    break;
                                case "DefaultPrice2":
                                    STOCK_PP = STOCKDATA?.DefaultPrice2;
                                    STOCK_SP = STOCKDATA?.SPrice5;
                                    break;
                                case "DefaultPrice3":
                                    STOCK_PP = STOCKDATA?.DefaultPrice3;
                                    STOCK_SP = STOCKDATA?.SPrice6;
                                    break;
                                case "DefaultPrice4":
                                    STOCK_PP = STOCKDATA?.DefaultPrice4;
                                    STOCK_SP = STOCKDATA?.SPrice7;
                                    break;
                                case "DefaultPrice5":
                                    STOCK_PP = STOCKDATA?.DefaultPrice5;
                                    STOCK_SP = STOCKDATA?.SPrice8;
                                    break;
                                default:
                                    STOCK_PP = 0;
                                    STOCK_PP = 0;
                                    break;
                            }

                            #endregion 找出牌價和建議售價

                            var Out20InsertData = new Out20
                            {
                                OutNo = vm.OutNo ?? "",
                                Serno = SERNO,
                                PartNo = partNO ?? "",
                                Qty = STOCK_QTY,
                                Price = STOCK_PRICE,
                                Amount = STOCK_AMOUNT,
                                Discount = STOCK_DIS,
                                Unit = STOCKDATA != null ? STOCKDATA.Unit : "",
                                Memo = PS[i] ?? "",
                                SPrice = STOCK_SP,
                                PPrice = STOCK_PP,
                                IsPromise = SALE.Contains(partNO) ? "1" : null,
                            };

                            _context.Out20.Add(Out20InsertData);
                            await _context.SaveChangesAsync();

                            var properties = typeof(Out20).GetProperties();
                            var logMsg = string.Join(",", properties.Select(x => $"{x.Name}={x.GetValue(Out20InsertData)}"));
                            logger.WriteLog(vm.CustomerId, logMsg);

                            // 計算總金額
                            if (STOCKDATA.TaxType == "免稅") { TOTAL0 += Math.Round(STOCK_AMOUNT, 2); }
                            if (STOCKDATA.TaxType == "應稅") { TOTAL1 += Math.Round(STOCK_AMOUNT, 2); }
                            TOTAL += Math.Round(STOCK_AMOUNT, 2);

                            KG += Convert.ToDouble(STOCK_QTY * STOCKDATA.InitQty2);

                            SERNO++;
                        }

                        i++;
                    }

                    #endregion 加入明細資料

                    // 計算異動資料
                    #region 計算異動資料

                    // 公斤
                    KG = KG != 0 ? (double)decimal.Round((decimal)KG / 1000, 2) : KG;
                    // 折扣
                    CASHDIS = Math.Round((TOTAL * DISCOUNT) / 100, 2);
                    // 未收 = 總計 - 已收 - 折扣 - 折讓
                    NOTGET = Math.Round(TOTAL - YESGET - CASHDIS - SUBTOT, 2);

                    // 補足主表資料
                    INSERTDATA.Kg = KG;
                    INSERTDATA.CashDis = CASHDIS;
                    INSERTDATA.NotGet = NOTGET;
                    INSERTDATA.Total = TOTAL;
                    INSERTDATA.Total1 = TOTAL1;
                    INSERTDATA.Total0 = TOTAL0;
                    INSERTDATA.SubTot = SUBTOT;
                    INSERTDATA.YesGet = YESGET;
                    INSERTDATA.Discount = DISCOUNT;
                    _context.SaveChanges();
                    HttpContext.Session.SetString("msg", "新增成功");

                    #endregion 計算異動資料

                    //清掉不必要的參數
                    string area = vm.Area;
                    string customerId = vm.CustomerId;
                    vm = new OrderEditViewModel
                    {
                        Area = area,
                        CustomerId = customerId,
                        OrderState = "4",
                    };

                    return RedirectToAction("Order", "Sales", vm);
                }
                return View(vm);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        //public async Task<IActionResult> OrderEdit(string id, List<string> SALE, List<string> PART_NO, List<string> SPEC, List<string> PORDNUM, List<string> PS, [Bind("ORD_ID,ORD_TIME,ORD_STATE,ORD_FTIME,ORD_CODE,ORD_PAYNONEY,ORD_RECMONEY,ORD_RECTIME,ORD_AREA,ORD_CLIENT,ORD_USERID,ORD_USERNAME,ORD_ORDER,ORD_LINKID,ORD_PS,ORD_PTIME")] STO_ORDER sTO_ORDER)
        public async Task<IActionResult> OrderEdit_old(OrderEditViewModel vm, List<string> SALE, List<string> PART_NO, List<string> SPEC, List<string> PORDPRICE, List<string> PORDNUM, List<string> PS)
        {
            try
            {
                //新增訂貨單(出貨單)主表
                if (ModelState.IsValid)
                {
                    //if (!string.IsNullOrEmpty(vm.IsCopy) && vm.IsCopy.Equals("true"))
                    //{
                    //    vm.OutNo = string.Empty;
                    //}

                    //新增
                    if (string.IsNullOrEmpty(vm.OutNo))
                    {
                        //取得當年當月最後一筆出貨單號
                        //var cond = DateTime.Now.ToString("yyyyMM");
                        // 改成5000開始
                        var cond = DateTime.Now.ToString("yyyMM") + "5";

                        var lastOutNo = _context.Out10
                            .Where(x => x.OutNo.Contains(cond))
                            .OrderByDescending(x => x.OutNo).FirstOrDefault()?.OutNo;

                        string addOutNo = string.Empty;

                        //如果當月份沒有的話，直接幫她新增
                        if (string.IsNullOrEmpty(lastOutNo))
                        {
                            //addOutNo = DateTime.Now.ToString("yyyyMM") + "0001";
                            // 改成5000開始
                            addOutNo = cond + "001";
                        }
                        else
                        {
                            addOutNo = (int.Parse(lastOutNo) + 1).ToString();
                        }

                        ////比對新增出貨單號，如果重複 取出當月份最新出貨單單號+1
                        //var outNoCount = _context.Out10
                        //    .Where(x => x.OutNo.Contains(cond)).Count();

                        //if (outNoCount == 0)
                        //{
                        //    outNoCount = 1;
                        //}

                        //var addOutNo = cond + outNoCount.ToString().PadLeft(4, '0');

                        //while (lastOutNo.OutNo == addOutNo)
                        //{
                        //    addOutNo = (int.Parse(addOutNo) + 1).ToString();
                        //}

                        //var getLastNum = _context.Out10.Where(x => x.CoNo == vm.CustomerId)
                        //     .OrderByDescending(x => x.OutNo).FirstOrDefault();

                        //var pKey = (int.Parse(getLastNum.OutNo) + 1).ToString();

                        //先新增出貨單主表
                        //TODO 補其他欄位資料
                        Out10 insertData = new Out10
                        {
                            OutNo = addOutNo,
                            //經由ipad產出的訂單狀態先給未出貨
                            OutType = "1",
                            OutDate = DateTime.Now.AddDays(1).ToString("yyyyMMdd"),
                            Paymonth = DateTime.Now.AddDays(25).ToString("yyyyMM"),
                            Ntus = "NTD",
                            CoNo = vm.CustomerId,
                            Memo = vm.Memo,
                            Userid = HttpContext.Session.GetString("UserAc"),
                            //PeNo = companyData.PeNo,
                            //DriveNo = companyData.DriveNo,
                            StockPass = null,
                            YnClose = null,
                            PeNo = vm.Business,

                            KeyinDate = DateTime.Now.ToString("yyyyMMdd"),
                            DriveNo = vm.Driver,
                            Tax = 0,
                            Promotion_No = null,
                            TallyState = "2"
                        };

                        // 需要計算的資料
                        double? need_total1 = 0;
                        double? need_cashdis = 0;
                        double? need_subtot = 0;
                        double? need_notget = 0;
                        double? need_yesget = 0;
                        double? need_total0 = 0;
                        double? need_total = 0;
                        double? need_discount = 0;
                        double? need_kg = 0;

                        //主表資料
                        var out10MainData = _context.Out10.Add(insertData);
                        await _context.SaveChangesAsync();

                        //判斷有無促銷
                        var promotionData = _context.Stock20
                            .Where(x => x.CoNo == out10MainData.Entity.CoNo)
                            .OrderByDescending(x => x.SpNo).FirstOrDefault();

                        var checkTime = false;

                        if (promotionData != null)
                        {
                            //如果有在比對日期是否在期限內
                            checkTime = (int.Parse(promotionData.Bdate) <= int.Parse(out10MainData.Entity.OutDate)
                                && int.Parse(promotionData.Edate) >= int.Parse(out10MainData.Entity.OutDate));

                            if (checkTime)
                            {
                                out10MainData.Entity.Promotion_No = promotionData.SpNo;
                            }

                        }

                        int fus = 0;
                        var serNo = 1;

                        //再新增出貨單明細
                        //TODO 促銷&販售狀況&其他欄位
                        foreach (var partNo in PART_NO)
                        {
                            if (Convert.ToInt32(PORDNUM[fus]) > 0)
                            {
                                //取出產品資料處理
                                var stockData = _context.Stock10.Where(x => x.PartNo == partNo).FirstOrDefault();

                                var stock_price = Convert.ToDouble(PORDPRICE[fus]) > 0 ? Convert.ToDouble(PORDPRICE[fus]) : 0;
                                var stock_qty = Convert.ToInt32(PORDNUM[fus]);
                                var stock_dis = stockData != null ? stockData.LastDiscount : 0;

                                var out20InsertData = new Out20
                                {
                                    OutNo = insertData.OutNo,
                                    Serno = serNo,
                                    PartNo = partNo,
                                    Qty = stock_qty,
                                    //Price = stockData.Price1,
                                    Price = stock_price,
                                    Memo = PS[fus],
                                    Unit = stockData.Unit,
                                    Amount = ((stock_price * stock_qty) * (100 - stock_dis)) / 100,
                                    Discount = stockData != null ? stockData.LastDiscount : 0
                                };

                                serNo++;

                                var out20MainData = _context.Out20.Add(out20InsertData);
                                //await _context.SaveChangesAsync();

                                // 判斷是否為約定價
                                if (SALE.Contains(partNo))
                                {
                                    var promisedata = _context.PROMISE_PRICE.Where(x => x.CO_NO == vm.CustomerId && x.PART_NO == partNo).OrderByDescending(x => x.PP_NO).FirstOrDefault();
                                    if (promisedata == null)
                                    {
                                        var promiseInsertData = new Promise_Price
                                        {
                                            ADD_TIME = DateTime.Now,
                                            PART_NO = partNo,
                                            CO_NO = vm.CustomerId,
                                            PRICE = Convert.ToDouble(PORDPRICE[fus]) > 0 ? Convert.ToDouble(PORDPRICE[fus]) : 0
                                        };
                                        var promiseMainData = _context.PROMISE_PRICE.Add(promiseInsertData);

                                        out20MainData.Entity.IsPromise = promiseMainData.Entity.PP_NO.ToString();
                                    }
                                    else
                                    {
                                        out20MainData.Entity.IsPromise = promisedata.PP_NO.ToString();
                                    }
                                }

                                //判斷有無促銷
                                //if (checkTime)
                                //{
                                //    var promotionDataDetail = _context.Stock21
                                //        .Where(x => x.SpNo == promotionData.SpNo).ToList();

                                //    if (promotionDataDetail != null)
                                //    {
                                //        foreach (var promotionDataDetailItem in promotionDataDetail)
                                //        {
                                //            if (promotionDataDetailItem.PartNo == partNo)
                                //            {
                                //                out20MainData.Entity.Price = promotionDataDetailItem.Newprice;
                                //            }
                                //        }
                                //    }
                                //}

                                //最後取出主表欄位做計算
                                //out10MainData.Entity.Total += out20InsertData.Qty * out20InsertData.Price;
                                need_total += out20InsertData.Amount;

                                //免稅金額(total0)
                                if (stockData.TaxType == "免稅")
                                {
                                    //out10MainData.Entity.Total += out20InsertData.Qty * out20InsertData.Price;
                                    need_total0 += out20InsertData.Amount;
                                }
                                else
                                {
                                    need_total1 += out20InsertData.Amount;
                                }

                                //主表重量相加
                                //out10MainData.Entity.Kg += out20InsertData.Qty * stockData.InitQty2;
                                need_kg += out20InsertData.Qty * stockData.InitQty2;

                                _context.SaveChanges();
                            }

                            fus++;
                        }

                        //如果有公斤資料的話，轉換單位
                        need_kg = need_kg != 0 ? (double?)decimal.Round((decimal)need_kg / 1000, 2) : need_kg;
                        //if (out10MainData.Entity.Kg != 0)
                        //{

                        //    var kg = out10MainData.Entity.Kg ?? 0;

                        //    out10MainData.Entity.Kg = (double?)decimal.Round((decimal)kg / 1000, 2);
                        //}
                        //else
                        //{
                        //    out10MainData.Entity.Kg = 0;
                        //}

                        //合計金額減掉免稅金額等於應稅金額
                        //out10MainData.Entity.Total1 = out10MainData.Entity.Total - out10MainData.Entity.Total0;

                        // 計算折扣、未收等
                        var cstm = _context.Cstm10.Find(vm.CustomerId);
                        need_discount = cstm != null ? (cstm.Discount ?? 0) : 0;
                        need_cashdis = (need_total * need_discount) / 100;
                        need_subtot = 0;
                        need_yesget = 0;
                        need_notget = need_total - need_yesget - need_cashdis - need_subtot;

                        out10MainData.Entity.Discount = need_discount;
                        out10MainData.Entity.CashDis = need_cashdis;
                        out10MainData.Entity.SubTot = need_subtot;
                        out10MainData.Entity.YesGet = need_yesget;
                        out10MainData.Entity.NotGet = need_notget;
                        out10MainData.Entity.Kg = need_kg;
                        out10MainData.Entity.Total0 = need_total0;
                        out10MainData.Entity.Total1 = need_total1;
                        out10MainData.Entity.Total = need_total;


                        _context.SaveChanges();

                        HttpContext.Session.SetString("msg", "新增成功");

                        //清掉不必要的參數
                        string area = vm.Area;
                        string customerId = vm.CustomerId;
                        vm = new OrderEditViewModel
                        {
                            Area = area,
                            CustomerId = customerId,
                            OrderState = "4",
                        };

                        return RedirectToAction("Order", "Sales", vm);
                    }
                    else
                    {

                        #region 修改

                        //TODO 欄位相關
                        //先抓出主表
                        var out10MainData = _context.Out10.Where(x => x.OutNo == vm.OutNo).FirstOrDefault();

                        int fus = 0;
                        var serNo = 1;

                        // 需要計算的資料
                        double? need_total1 = 0;
                        double? need_cashdis = 0;
                        double? need_subtot = 0;
                        double? need_notget = 0;
                        double? need_yesget = 0;
                        double? need_total0 = 0;
                        double? need_total = 0;
                        double? need_discount = 0;
                        double? need_kg = 0;


                        //先刪除out20相關資料

                        var deleteOut20Data = _context.Out20.Where(x => x.OutNo == out10MainData.OutNo).ToList();

                        //因為先刪除detail 所以先將主表總價歸零
                        out10MainData.Total = 0;
                        _context.Out20.RemoveRange(deleteOut20Data);

                        _context.SaveChanges();

                        foreach (var partNo in PART_NO)
                        {
                            if (Convert.ToInt32(PORDNUM[fus]) > 0)
                            {
                                //取出產品資料處理
                                var stockData = _context.Stock10.Where(x => x.PartNo == partNo).FirstOrDefault();

                                var stock_price = Convert.ToDouble(PORDPRICE[fus]) > 0 ? Convert.ToDouble(PORDPRICE[fus]) : 0;
                                var stock_qty = Convert.ToInt32(PORDNUM[fus]);
                                var stock_dis = stockData != null ? stockData.LastDiscount : 0;

                                var insertOut20Data = new Out20
                                {
                                    OutNo = out10MainData.OutNo,
                                    Serno = serNo,
                                    PartNo = partNo,
                                    Qty = stock_qty,
                                    //Price = stockData.Price1,
                                    Price = stock_price,
                                    Memo = PS[fus],
                                    Unit = stockData.Unit,
                                    Amount = ((stock_price * stock_qty) * (100 - stock_dis)) / 100,
                                    Discount = stockData != null ? stockData.LastDiscount : 0
                                };
                                serNo++;

                                var out20MainData = _context.Out20.Add(insertOut20Data);

                                //判斷是否為約定價
                                if (SALE.Contains(partNo))
                                {
                                    var promisedata = _context.PROMISE_PRICE.Where(x => x.CO_NO == vm.CustomerId && x.PART_NO == partNo).OrderByDescending(x => x.PP_NO).FirstOrDefault();
                                    if (promisedata == null)
                                    {
                                        var promiseInsertData = new Promise_Price
                                        {
                                            ADD_TIME = DateTime.Now,
                                            PART_NO = partNo,
                                            CO_NO = vm.CustomerId,
                                            PRICE = Convert.ToDouble(PORDPRICE[fus]) > 0 ? Convert.ToDouble(PORDPRICE[fus]) : 0
                                        };
                                        var promiseMainData = _context.PROMISE_PRICE.Add(promiseInsertData);

                                        out20MainData.Entity.IsPromise = promiseMainData.Entity.PP_NO.ToString();
                                    }
                                    else
                                    {
                                        out20MainData.Entity.IsPromise = promisedata.PP_NO.ToString();
                                    }
                                }

                                //最後取出主表欄位做計算
                                //out10MainData.Entity.Total += out20InsertData.Qty * out20InsertData.Price;
                                need_total += insertOut20Data.Amount;

                                //免稅金額(total0)
                                if (stockData.TaxType == "免稅")
                                {
                                    //out10MainData.Entity.Total += out20InsertData.Qty * out20InsertData.Price;
                                    need_total0 += insertOut20Data.Amount;
                                }
                                else
                                {
                                    need_total1 += insertOut20Data.Amount;
                                }

                                //主表重量相加
                                //out10MainData.Entity.Kg += out20InsertData.Qty * stockData.InitQty2;
                                need_kg += insertOut20Data.Qty * stockData.InitQty2;

                                _context.SaveChanges();
                            }
                            fus++;
                        }

                        out10MainData.Userid = HttpContext.Session.GetString("UserAc");

                        //如果有公斤資料的話，轉換單位
                        need_kg = need_kg != 0 ? (double?)decimal.Round((decimal)need_kg / 1000, 2) : need_kg;

                        //合計金額減掉免稅金額等於應稅金額
                        // 計算折扣、未收等
                        var cstm = _context.Cstm10.Find(vm.CustomerId);
                        need_discount = out10MainData.Discount;
                        need_cashdis = (need_total * need_discount) / 100;
                        need_subtot = out10MainData.SubTot;
                        need_yesget = out10MainData.YesGet;
                        need_notget = need_total - need_yesget - need_cashdis - need_subtot;

                        out10MainData.Discount = need_discount;
                        out10MainData.CashDis = need_cashdis;
                        out10MainData.SubTot = need_subtot;
                        out10MainData.YesGet = need_yesget;
                        out10MainData.NotGet = need_notget;
                        out10MainData.Kg = need_kg;
                        out10MainData.Total0 = need_total0;
                        out10MainData.Total1 = need_total1;
                        out10MainData.Total = need_total;
                        out10MainData.Ntus = "NTD";
                        out10MainData.Memo = vm.Memo;
                        out10MainData.Userid = HttpContext.Session.GetString("UserAc");



                        _context.SaveChanges();

                        #endregion

                        //清掉不必要的參數
                        string area = vm.Area;
                        string customerId = vm.CustomerId;
                        vm = new OrderEditViewModel
                        {
                            Area = area,
                            CustomerId = customerId,
                            OrderState = "4",
                        };

                        HttpContext.Session.SetString("msg", "修改成功");

                        return RedirectToAction("Order", "Sales", vm);
                    }


                }
                return View(vm);
            }
            catch (Exception)
            {

                throw;
            }
        }

        [HttpGet]
        public async Task<IActionResult> OrderCopy(string OutNo, string Area, string CustomerId, string IsCopy)
        {
            try
            {
                if (string.IsNullOrEmpty(OutNo))
                {
                    return NotFound();
                }

                OrderEditViewModel vm = new OrderEditViewModel();

                vm.Area = Area;
                vm.CustomerId = CustomerId;
                if (!string.IsNullOrEmpty(CustomerId))
                {
                    vm.CoName = _context.Cstm10.Where(x => x.CoNo == CustomerId).FirstOrDefault().Coname;
                }
                vm.IsCopy = IsCopy;

                //先取分類列表
                var title = _context.Stock10.Where(x => x.IsOpen == true).Select(x => new StockTitle
                {
                    Name = x.Type
                }).Distinct().OrderBy(x => x.Name).ToList();

                vm.StockTitleList = title;

                //取出出貨單主表
                var outOrderMain = _context.Out10.Where(x => x.OutNo == OutNo).FirstOrDefault();

                if (outOrderMain != null)
                {
                    vm.CustomerId = outOrderMain.CoNo;
                    vm.CoName = _context.Cstm10.Where(x => x.CoNo == outOrderMain.CoNo).FirstOrDefault().Coname;
                }

                //TODO 訂單紀錄(單筆)By Alex
                #region 訂單紀錄(單筆)

                var outOrderDetailData = _context.Out20
                    .Where(x => x.OutNo == (vm != null ? OutNo : string.Empty)).ToList();

                vm.OutDetail = new List<OutOrderDetail>();

                foreach (var item in outOrderDetailData)
                {
                    var OutDetail = new OutOrderDetail();

                    OutDetail.OutDetailNo = item.OutNo;
                    OutDetail.PartNo = item.PartNo;
                    OutDetail.Sale = false;
                    OutDetail.Stats = false;
                    //品名
                    OutDetail.Name = _context.Stock10.Where(x => x.PartNo == item.PartNo).Select(x => x.Spec).ToString();
                    OutDetail.Qty = item.Qty;
                    OutDetail.Memo = item.Memo;

                    vm.OutDetail.Add(OutDetail);
                }

                #endregion 訂單紀錄(單筆)

                #region 新增/修改

                var promotionData = new Stock20();
                var checkTime = false;

                //複製新訂單
                ViewBag.FUNNAME = "複製新訂單";

                vm.OrderTime = DateTime.ParseExact(outOrderMain.OutDate, "yyyyMMdd", null).ToString("yyyy-MM-dd");
                vm.Memo = outOrderMain.Memo;

                //檢查是否有促銷資料
                if (outOrderMain.Promotion_No != null)
                {
                    checkTime = true;
                }

                #endregion

                //TODO 促銷 販售&狀態待確認 By Alex
                #region 寫入資料

                //先找出所有訂單
                //var allOut10Data = await _context.Out10.Where(x => x.CoNo == CustomerId).Select(x => x.OutNo).ToListAsync();

                var allData = (from out10 in _context.Out10
                               join out20 in _context.Out20 on out10.OutNo equals out20.OutNo
                               where out10.CoNo.Equals(vm.CustomerId)
                               select new { out20.PartNo }).Distinct().ToList();

                Dictionary<int, string> dict = new Dictionary<int, string>();

                if (allData.Count() > 0)
                {
                    for (int i = 0; i < allData.Count(); i++)
                    {
                        dict.Add(i, allData[i].PartNo);
                    }
                }

                vm.StockList = new List<StockInfo>();

                foreach (var titleItem in title)
                {
                    var stockData = await _context.Stock10
                        .Where(x => x.Type == titleItem.Name)
                        .Select(x => new StockInfo
                        {
                            PartNo = x.PartNo,
                            Spec = x.Spec,
                            StockType = x.Type
                        }).OrderBy(x => x.PartNo).ToListAsync();

                    foreach (var stock in stockData)
                    {
                        if (dict.Where(x => x.Value == stock.PartNo).Any())
                        {
                            stock.IsSalesStatus = true;
                        }

                        //塞入促銷單對應資料
                        if (checkTime)
                        {
                            var promotionDataDetail = _context.Stock21.Where(x => x.SpNo == outOrderMain.Promotion_No).ToList();

                            if (promotionDataDetail != null)
                            {
                                foreach (var promotionDataDetailItem in promotionDataDetail)
                                {
                                    if (promotionDataDetailItem.PartNo == stock.PartNo)
                                    {
                                        stock.IsPromotion = true;
                                    }
                                }
                            }
                        }

                        //20221224 item2.PackQty = null;
                        if (stock.PackQty == null)//數量
                            stock.PackQty = null;
                        else
                            stock.PackQty = null;

                        stock.StPs = "";

                        if (outOrderDetailData != null)
                        {
                            if (vm.OutDetail != null)
                            {
                                foreach (var outDetailItem in vm.OutDetail.Where(x => x.PartNo == stock.PartNo))
                                {
                                    stock.PackQty = outDetailItem.Qty;
                                    stock.StPs = outDetailItem.Memo;
                                }
                            }
                        }
                    }

                    vm.StockList.AddRange(stockData);
                }

                if (vm.StockList != null)
                {
                    vm.StockList = vm.StockList
                        .OrderByDescending(x => x.IsPromotion)
                        .OrderByDescending(x => x.IsSalesStatus)
                        .ToList();
                }

                #endregion 寫入資料

                return View("OrderEdit", vm);
            }
            catch (Exception)
            {

                throw;
            }
        }

        // GET: Sales/Delete/5
        public async Task<IActionResult> OrderDel(string id, string Area, string CustomerId)
        {
            try
            {
                if (id == null)
                {
                    return NotFound();
                }

                //先找出主表資料
                var deleteData = _context.Out10.Where(x => x.OutNo == id).FirstOrDefault();

                if (deleteData == null)
                {
                    return NotFound();
                }

                //已出貨不能刪除
                if (deleteData.OutType == "2")
                {
                    //再找出明細清單
                    var deleteOut20Data = _context.Out20.Where(x => x.OutNo == deleteData.OutNo).ToList();

                    if (deleteOut20Data != null)
                    {
                        //先刪除明細資烙
                        _context.Out20.RemoveRange(deleteOut20Data);

                        //再刪除主表資料
                        _context.Out10.Remove(deleteData);

                        await _context.SaveChangesAsync();
                    }

                    return RedirectToAction("Order", "Sales", new OrderViewModel
                    {
                        Area = Area,
                        CustomerId = CustomerId,
                        OrderState = "4",
                    });
                }

                //設定初始資料

                var vm = new OrderViewModel
                {
                    Area = Area,
                    CustomerId = CustomerId,
                    OrderState = "4",
                };

                return RedirectToAction("Order", "Sales", vm);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public Dictionary<string, double?> GetStockPrice(string CoNo, List<string> PartNos)
        {

            var result = new Dictionary<string, double?>();

            var Today = DateTime.Today.ToString("yyyyMMdd");

            // 此客戶的所有出貨單
            var OUT10 = _context.Out10.Where(x => x.CoNo == CoNo && x.OutType == "1").Select(x => new { CoNo = x.CoNo, OutNo = x.OutNo, KeyInDate = x.KeyinDate }).ToList();
            //找出在促銷期間單號
            var SalesHistory_OUTNO = OUT10.Join(_context.Stock20, x => x.CoNo, y => y.CoNo, (x, y) => new
            {
                OUTNO = x.OutNo,
                KeyInDate = x.KeyInDate,
                BDATE = y.Bdate,
                EDATE = y.Edate,
            }).Where(z => String.Compare(z.BDATE, z.KeyInDate) <= 0 && String.Compare(z.KeyInDate, z.EDATE) >= 0).Select(z => z.OUTNO).ToList();

            if (!String.IsNullOrEmpty(CoNo) && PartNos != null && PartNos.Count() > 0)
            {
                var CSTM = _context.Cstm10.Find(CoNo);

                var CSTM_PriceType = CSTM?.PriceType;
                var STOCKCOL = _context.PRICE_TYPE.Where(x => x.PT_VALUE == CSTM_PriceType).FirstOrDefault()?.PT_STOCKCOL;

                foreach (var PartNo in PartNos)
                {
                    var STOCK = _context.Stock10.Find(PartNo);

                    // 促銷價
                    var Stock20 = _context.Stock20.Where(x => x.CoNo == CoNo && String.Compare(Today, x.Bdate) >= 0 && String.Compare(Today, x.Edate) <= 0).OrderByDescending(x => x.SpNo).Select(x => x.SpNo).ToList();
                    var Stock21 = _context.Stock21.Where(x => Stock20.Contains(x.SpNo)).OrderByDescending(x => x.SpNo).FirstOrDefault();
                    if (Stock21 != null)
                    {
                        result.Add(PartNo, Stock21?.Newprice);
                        continue;
                    }

                    // 找出最新歷史資料
                    var LastOut20 = _context.Out20.Join(_context.Out10, x => x.OutNo, y => y.OutNo, (x, y) => new
                    {
                        OutNO = x.OutNo,
                        PartNo = x.PartNo,
                        CoNo = y.CoNo,
                        Price = x.Price,
                        KeyInDate = y.KeyinDate,
                        OutType = y.OutType
                    }).Where(z => z.CoNo == CoNo && z.OutType == "1").OrderByDescending(x => x.OutNO).FirstOrDefault();

                    //找出不在促銷期間的並且有此產品的最新單號和價格
                    var OUT20 = _context.Out20.Where(x => !SalesHistory_OUTNO.Contains(x.OutNo) && OUT10.Select(x => x.OutNo).ToList().Contains(x.OutNo) && x.PartNo == PartNo).OrderByDescending(x => x.OutNo).FirstOrDefault();
                    // 獲取歷史訂單的建立時間
                    var HISTORY_OUTTIME = _context.Out10.Find(OUT20?.OutNo)?.OutDate;
                    // 先獲取比歷史訂單新且有此產品的報價單
                    var BOUDATA = _context.Bou10.Join(_context.Bou20, x => x.QuNo, y => y.QuNo, (x, y) => new
                    {
                        QUNO = x.QuNo,
                        PARTNO = y.PartNo,
                        CONO = x.CoNo,
                        QUDATE = x.QuDate,
                        SENDDATE = x.SendDate,
                        PRICE = y.Price
                    }).Where(z => z.CONO == CoNo && String.Compare(z.SENDDATE, Today) <= 0 && z.PARTNO == PartNo && String.Compare(z.QUDATE, HISTORY_OUTTIME) >= 0).OrderByDescending(x => x.QUNO).FirstOrDefault();

                    if (BOUDATA != null)
                    {
                        result.Add(PartNo, BOUDATA.PRICE);
                        continue;
                    }

                    if (OUT20 != null)
                    {
                        result.Add(PartNo, OUT20.Price);
                        continue;
                    }

                    // 牌價
                    switch (STOCKCOL)
                    {
                        case "Price1":
                            result.Add(PartNo, STOCK?.Price1);
                            break;
                        case "Price2":
                            result.Add(PartNo, STOCK?.Price2);
                            break;
                        case "Price3":
                            result.Add(PartNo, STOCK?.Price3);
                            break;
                        case "DefaultPrice1":
                            result.Add(PartNo, STOCK?.DefaultPrice1);
                            break;
                        case "DefaultPrice2":
                            result.Add(PartNo, STOCK?.DefaultPrice2);
                            break;
                        case "DefaultPrice3":
                            result.Add(PartNo, STOCK?.DefaultPrice3);
                            break;
                        case "DefaultPrice4":
                            result.Add(PartNo, STOCK?.DefaultPrice4);
                            break;
                        case "DefaultPrice5":
                            result.Add(PartNo, STOCK?.DefaultPrice5);
                            break;
                        default:
                            result.Add(PartNo, 0);
                            break;
                    }
                }

            }

            return result;
        }
    }
}
