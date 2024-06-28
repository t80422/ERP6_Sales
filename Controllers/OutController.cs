using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERP6.Models;
using ERP6.ViewModels.Out;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;

namespace ERP6.Controllers
{
    public class OutController : Controller
    {
        private readonly EEPEF01Context _context;

        public OutController(EEPEF01Context context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(OutIndexViewModel vm , int? page = 1)
        {
            try
            {
                vm.tallyState = vm.tallyState ?? "4";

                // 只保存一個禮拜的資料
                var Today = DateTime.Today.ToString("yyyyMMdd") + "2359"; // 今天
                var BeforeAWeek = DateTime.Today.AddDays(-6).ToString("yyyyMMdd") + "0000"; // 一個禮拜
                //所有一個禮拜內的訂單
                var orderData = await _context.Out10.Join(_context.Cstm10, x => x.CoNo, y => y.CoNo, (x, y) => new TallyList
                {
                    OrderState = x.OutType ?? "",
                    OrderNo = x.OutNo ?? "",
                    OrderTime = !String.IsNullOrEmpty(x.KeyinDate) ? DateTime.ParseExact((x.KeyinDate).PadRight(12,'0'), "yyyyMMddHHmm", null).ToString("yyyy-MM-dd HH:mm") : "",
                    OrderAmount = (double)Math.Round((decimal)x.Total, 2),
                    KeyInDate = x.KeyinDate,
                    //Driver = "",
                    //DriverName = "",
                    CoName = y.Coname ?? "",
                    CustomerId = x.CoNo ?? "",
                    Area = y.AreaNo ?? "",
                    TallyState = x.TallyState ?? "2",
                }).Where(z => z.KeyInDate != ""
                         && String.Compare(z.KeyInDate, Today) <= 0
                         && String.Compare(z.KeyInDate, BeforeAWeek) >= 0
                         && z.OrderState == "1"
                         ).OrderByDescending(z => z.OrderNo).ToListAsync();

                // 搜尋條件
                // 訂單狀態
                if (!String.IsNullOrEmpty(vm.tallyState) && vm.tallyState != "4")
                {
                    orderData = await orderData.Where(x => x.TallyState == vm.tallyState).ToListAsync();
                }
                    
                // 訂單編號
                if (!String.IsNullOrEmpty(vm.OrderNo))
                {
                    orderData = await orderData.Where(x => x.OrderNo.Contains(vm.OrderNo)).ToListAsync();
                }
                    
                // 訂購時間
                if (!String.IsNullOrEmpty(vm.OrderTime))
                {
                    var keyindate = DateTime.ParseExact(vm.OrderTime, "yyyy-MM-dd", null).ToString("yyyyMMdd");
                    orderData = await orderData.Where(x => (x.KeyInDate).Substring(0,8) == keyindate).ToListAsync();
                }
                    
                // 司機
                if (!String.IsNullOrEmpty(vm.Driver))
                {
                    orderData = await orderData.Where(x => x.Driver == vm.Driver).ToListAsync();
                }
                    
                // 區域
                if (!String.IsNullOrEmpty(vm.Area))
                {
                    orderData = await orderData.Where(x => x.Area == vm.Area).ToListAsync();
                }
                // 客戶
                if (!String.IsNullOrEmpty(vm.CustomerId))
                {
                    orderData = await orderData.Where(x => x.CustomerId == vm.CustomerId).ToListAsync();
                }

                // 區域
                ViewBag.Area = new SelectList(_context.STO_AREA.Where(x => x.AREA_STATE == 1).ToList(), "AREA_NAME", "AREA_NAME", vm.Area);
                // 客戶
                ViewBag.Client = new SelectList(_context.Cstm10.Where(x => x.AreaNo == vm.Area).ToList(), "CoNo", "Coname", string.Empty);
                // 訂單狀態
                var tallystateList = new List<SelectListItem>()
                {
                    new SelectListItem{Text="全部訂單",Value="4"},
                    new SelectListItem{Text="未理貨訂單",Value="2"},
                    new SelectListItem{Text="已理貨訂單",Value="1"},
                };
                ViewBag.StatesList = new SelectList(tallystateList, "Value", "Text", vm.tallyState);
                // 司機人員
                ViewBag.DriverList = new SelectList(_context.Pepo10.Where(x => x.Posi == "司機").ToList(), "PeNo", "Name", vm.Driver);

                if (vm.IsSearch)
                {
                    vm.tallied = orderData.Where(x => x.TallyState == "1").Count();
                    vm.Nottallied = orderData.Where(x => x.TallyState == "2" || String.IsNullOrEmpty(x.TallyState)).Count();
                    vm.tallyList = await orderData.ToPagedListAsync((int)page, 20);
                }
                else
                {
                    orderData = orderData.Where(x => (x.KeyInDate).Substring(0,8) == DateTime.Today.ToString("yyyyMMdd")).ToList();
                    vm.tallied = orderData.Where(x => x.TallyState == "1").Count();
                    vm.Nottallied = orderData.Where(x => x.TallyState == "2" || String.IsNullOrEmpty(x.TallyState)).Count();
                    vm.tallyList = await orderData.ToPagedListAsync((int)page, orderData.Count() + 1);
                }
                return View(vm);
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public async Task<IActionResult> IndexNotUse(OutIndexViewModel vm , int? page = 1)
        {
            try
            {
                // 先 Join Out10 和 Customer 獲取顧客資料
                var tallyData = await (
                            _context.Out10.Join(_context.Cstm10, x => x.CoNo, y => y.CoNo, (x, y) => new {
                                OrderNo = x.OutNo, 
                                OrderState = x.OutType,
                                OrderTime = DateTime.ParseExact(x.OutDate , "yyyyMMdd" , null).ToString("yyyy-MM-dd"),
                                OrderAmount = x.Total,
                                CoNo = x.CoNo,
                                CustomerId = x.CoNo,
                                CoName = y.Coname,
                                OutType = x.TallyState ?? "2",
                                Driver = x.DriveNo,
                                AreaNo = y.AreaNo,
                                OutDate = x.OutDate,
                                DriverName = _context.Pepo10.Where(a=>a.PeNo == x.DriveNo).FirstOrDefault().Name,
                                TallyState = x.TallyState ?? "2",
                            }).Where(z=>z.OrderState == "1").OrderByDescending(z=>z.OutDate).ThenByDescending(z=>z.OrderNo).ToListAsync()
                        ) ;

                if (tallyData == null) return NotFound();

                // 建立頁面資料

                var data = new OutIndexViewModel
                {
                    // 預設為全部訂單
                    tallyState = string.IsNullOrEmpty(vm.tallyState) ? "4" : vm.tallyState
                };

                // 建立搜尋下拉的資料
                // 區域
                ViewBag.Area = new SelectList(_context.STO_AREA.Where(x => x.AREA_STATE == 1).ToList(), "AREA_NAME", "AREA_NAME", data.Area);
                // 客戶
                ViewBag.Client = new SelectList(_context.Cstm10.Where(x => x.AreaNo == vm.Area).ToList(), "CoNo", "Company", string.Empty);
                // 訂單狀態
                var tallystateList = new List<SelectListItem>()
                {
                    new SelectListItem{Text="全部訂單",Value="4"},
                    new SelectListItem{Text="未理貨訂單",Value="2"},
                    new SelectListItem{Text="已理貨訂單",Value="1"},
                };
                ViewBag.StatesList = new SelectList(tallystateList, "Value", "Text", data.tallyState);
                // 司機人員
                ViewBag.DriverList = new SelectList(_context.Pepo10.Where(x => x.Posi == "司機").ToList(), "PeNo", "Name", data.Driver);

                // 建立下方訂單資料
                var tallyList = await tallyData.Select(x => new TallyList
                {
                    OrderState = x.OrderState,
                    OrderNo = x.OrderNo,
                    OrderTime = x.OrderTime,
                    OrderAmount = (double)Math.Round((decimal)(x.OrderAmount ?? 0),2),
                    KeyInDate = "",
                    Driver = x.Driver,
                    DriverName = x.DriverName,
                    CoName = x.CoName,  
                    CustomerId = x.CustomerId,
                    Area = x.AreaNo,
                    TallyState = x.TallyState,
                    OutType = x.OutType 
                }).ToListAsync();

                // 搜尋條件
                // 訂單狀態
                if (!String.IsNullOrEmpty(data.tallyState) && data.tallyState != "4")
                    tallyList = await tallyList.Where(x => x.TallyState == vm.tallyState).ToListAsync();
                // 訂單編號
                if (!String.IsNullOrEmpty(vm.OrderNo))
                    tallyList = await tallyList.Where(x => x.OrderNo.Contains(vm.OrderNo)).ToListAsync();
                // 訂購時間
                if (!String.IsNullOrEmpty(vm.OrderTime))
                    tallyList = await tallyList.Where(x => x.OrderTime.Contains(vm.OrderTime)).ToListAsync();
                // 司機
                if (!String.IsNullOrEmpty(vm.Driver))
                    tallyList = await tallyList.Where(x => x.Driver == vm.Driver).ToListAsync();
                // 區域
                if (!String.IsNullOrEmpty(vm.Area))
                    tallyList = await tallyList.Where(x => x.Area == vm.Area).ToListAsync();
                // 客戶
                if (!String.IsNullOrEmpty(vm.CustomerId))
                    tallyList = await tallyList.Where(x => x.CustomerId == vm.CustomerId).ToListAsync();

                // 加入資料
                data.tallyList = await tallyList.ToPagedListAsync((int)page, 10);
                data.AllTally = false;

                if (data.tallyList.Where(x => x.TallyState == "2" || x.TallyState == null).Count() == 0) data.AllTally = true;

                if (tallyData == null) return View(vm);

                // 已理貨總數
                data.tallied = tallyList.Where(x => x.OutType == "1").Count();
                // 未理貨總數
                data.Nottallied = tallyList.Where(x => x.OutType == "2").Count();

                return View(data);
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public async Task<IActionResult> Edit(string OutNo , OutIndexViewModel search)
        {
            ViewBag.Search_ts = search.tallyState ?? "";
            ViewBag.Search_on = search.OrderNo ?? "";
            ViewBag.Search_ot = search.OrderTime ?? "";
            ViewBag.Search_d = search.Driver ?? "";
            ViewBag.Search_a = search.Area ?? "";
            ViewBag.Search_c = search.CustomerId ?? "";
            ViewBag.Search_se = search.IsSearch ;
            var vm = new OutEditViewMidel();

            var Order = _context.Out10.Where(x => x.OutNo == OutNo).FirstOrDefault();
            var Area = search.Area ?? "";

            vm.OutNo = Order !=null ? Order.OutNo ?? "" : "";
            vm.CoName = Order != null ? (!String.IsNullOrEmpty(Order.CoNo) ? _context.Cstm10.Where(x => x.CoNo == Order.CoNo).FirstOrDefault().Coname : "") : "";
            vm.OrderState = Order != null ? Order.OutType ?? "" : "";
            vm.TallyState = Order != null ? Order.TallyState ?? "2" : "";
            vm.OrderTime = Order != null ? DateTime.ParseExact(Order.OutDate, "yyyyMMdd", null).ToString("yyyy-MM-dd") : "";
            vm.Area = Area != null ? Area : (Order != null ? (!String.IsNullOrEmpty(Order.CoNo) ? _context.Cstm10.Where(x => x.CoNo == Order.CoNo).FirstOrDefault().AreaNo : "") : "");
            vm.CustomerId = Order != null ? Order.CoNo ?? "" : "";
            vm.Memo = Order != null ? Order.Memo ?? "" : "";
            vm.Driver = Order != null ? Order.DriveNo ?? "" : "";
            vm.TallyId = Order != null ? (_context.Users.Where(x => x.Userid == Order.TALLYID).FirstOrDefault()?.Username) : "";
            // 商品分類列表
            //vm.StockTitleList = await (_context.Stock10.Where(x => x.IsOpen == true).Select(x => new StockTitle { Name = x.Type }).Distinct().OrderBy(x => x.Name).ToListAsync());
            // 改成有訂購的商品分類列表
            //vm.StockTitleList = await (_context.Out20.Where(x => x.OutNo == vm.OutNo).Join(_context.Stock10.Where(y=>y.IsOpen == true), x => x.PartNo, y => y.PartNo, (x, y) => new StockTitle { Name = y.Type }).Distinct().OrderBy(z => z.Name).ToListAsync());
            // 改成分類單中有開啟並有訂購的分類
            // 獲取開啟的分類
            var opentypes = _context.Stock_Type.Where(x => x.TYPE_ISOPEN == true).OrderBy(x => x.TYPE_ORDER).Select(x=>x.TYPE_NAME).ToList();
            vm.StockTitleList = await (_context.Out20.Where(x => x.OutNo == vm.OutNo).Join(_context.Stock10.Where(y => y.IsOpen == true && opentypes.Contains(y.Type) ), x => x.PartNo, y => y.PartNo, (x, y) => new StockTitle { Name = y.Type }).Distinct().OrderBy(z => z.Name).ToListAsync());
            // 訂單的產品
            vm.OutDetail = await(_context.Out20.Where(x => x.OutNo == vm.OutNo).Select(x => new OutOrderDetail
            {
                OutDetailNo = x.OutNo,
                PartNo = x.PartNo,
                Sale = !String.IsNullOrEmpty(x.IsPromise),
                Stats = false,
                Name = _context.Stock10.Where(y => y.PartNo == x.PartNo).FirstOrDefault() != null ? _context.Stock10.Where(y => y.PartNo == x.PartNo).FirstOrDefault().Spec : "",
                //Name = _context.Stock10.Find(x.PartNo).Spec,
                Qty = x.Qty,
                Memo = x.Memo,
            }).ToListAsync());

            var LAST_STOCK = _context.Out20.Join(_context.Out10.Where(x => x.CoNo == vm.CustomerId && String.Compare(x.OutNo, OutNo) <= 0 && x.OutNo != OutNo && x.OutType == "1")
                , x => x.OutNo, y => y.OutNo, (x, y) => new { PartNo = x.PartNo, Qty = x.Qty })
                .Where(x => x.Qty > 0).Select(x => x.PartNo).Distinct().ToList();


            var promotionData = new Stock20();

            // 促銷 販售&狀態待確認
            // 此訂單的所有產品
            var allDetails = vm.OutDetail != null ? vm.OutDetail.Select(x => x.PartNo).Distinct().ToList() : null;

            // 所有產品資料
            //vm.StockList = await _context.Stock10.Where(x => x.IsOpen == true).Select(x => new StockInfo
            //{
            //    PartNo = x.PartNo ,
            //    Spec = x.Spec ,
            //    StockType = x.Type
            //}).OrderBy(y=>y.PartNo).ToListAsync();

            // 改成只顯示有訂購的商品
            vm.StockList = await _context.Stock10.Where(x => x.IsOpen == true && allDetails.Contains(x.PartNo)).Select(x => new StockInfo
            {
                PartNo = x.PartNo,
                Spec = x.Spec,
                StockType = x.Type,
                stQty = x.StQty,
            }).OrderBy(y => y.PartNo).ToListAsync();

            vm.StockList.ForEach(delegate (StockInfo stock)
            {
                //if (allDetails.IndexOf(stock.PartNo) > -1) stock.IsSalesStatus = true;
                stock.IsSalesStatus = LAST_STOCK.Contains(stock.PartNo);               

                if (allDetails != null)
                {
                    var order_stock = vm.OutDetail.Where(x => x.PartNo == stock.PartNo).FirstOrDefault();
                    stock.PackQty = order_stock != null ? order_stock.Qty : null;
                    stock.StPs = order_stock != null ? order_stock.Memo : "";
                    stock.IsPromotion = order_stock.Sale;
                }
                else
                {
                    stock.PackQty = null;
                    stock.StPs = "";
                    stock.IsPromotion = false;
                }
            });

            return View(vm);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(OutEditViewMidel vm , List<string> SALE , List<string> PART_NO , List<string> SPEC , List<string> PORDNUM, List<string> PS)
        {
            try
            {

                if (ModelState.IsValid)
                {
                    // 先抓出訂單資訊
                    var outData = _context.Out10.Where(x => x.OutNo == vm.OutNo).FirstOrDefault();

                    // 先將之前的商品明細清除
                    //var DeloutDetails = _context.Out20.Where(x => x.OutNo == outData.OutNo).ToList();
                    //_context.Out20.RemoveRange(DeloutDetails);
                    //_context.SaveChanges();

                    // 因為刪除明細所以將訂單總價歸0
                    outData.Total = 0;

                    // 更改明細數量
                    for(var i = 0; i < PART_NO.Count; i++)
                    {
                        // 找出資料
                        var out20Data = _context.Out20.Where(x => x.OutNo == vm.OutNo && x.PartNo == PART_NO[i]).FirstOrDefault();

                        if(out20Data != null)
                        {
                            // 如果數量大於0
                            if (Convert.ToInt32(PORDNUM[i]) > 0)
                            {
                                // 取出產品資料
                                var stockData = _context.Stock10.Where(x => x.PartNo == PART_NO[i]).FirstOrDefault();

                                out20Data.Qty = Convert.ToInt32(PORDNUM[i]);
                                out20Data.Amount = out20Data.Price * Convert.ToInt32(PORDNUM[i]);

                                outData.Total += out20Data.Price * Convert.ToInt32(PORDNUM[i]);

                                if (stockData.TaxType == "免稅")
                                {
                                    outData.Total0 += out20Data.Qty * out20Data.Price;

                                }

                                //主表重量相加
                                outData.Kg += out20Data.Qty * stockData.InitQty2;

                                _context.SaveChanges();
                            }
                            else
                            {
                                // 數量0 刪除
                                _context.Out20.Remove(out20Data);
                                _context.SaveChanges();
                            }


                        }
                    }

                    
                    //// 不明的serNo
                    //var serNo = 1;
                    //// 跑新的明細迴圈
                    //for(var i = 0; i < PART_NO.Count; i++)
                    //{
                    //    // 如果數量大於0
                    //    if (Convert.ToInt32(PORDNUM[i]) > 0)
                    //    {
                    //        // 取出產品資料
                    //        var stockData = _context.Stock10.Where(x => x.PartNo == PART_NO[i]).FirstOrDefault();

                    //        // 加入詳細資料
                    //        var insertOut20 = new Out20
                    //        {
                    //            OutNo = outData.OutNo,
                    //            Serno = serNo,
                    //            PartNo = PART_NO[i],
                    //            Qty = Convert.ToInt32(PORDNUM[i]),
                    //            Price = stockData.Price1,
                    //            Amount = stockData.Price1 * Convert.ToInt32(PORDNUM[i]),
                    //            Memo = PS[i],
                    //            Unit = stockData.Unit,
                    //            IsPromise = SALE.Contains(PART_NO[i]) ? "1" : null
                    //        };
                    //        serNo++;
                    //        var out20MainData = _context.Out20.Add(insertOut20);

                    //        //最後取出主表欄位做計算(合計金額)
                    //        outData.Total += insertOut20.Qty * insertOut20.Price;

                    //        //免稅金額(total0)
                    //        if (stockData.TaxType == "免稅")
                    //        {
                    //            outData.Total0 += insertOut20.Qty * insertOut20.Price;

                    //        }

                    //        //主表重量相加
                    //        outData.Kg += insertOut20.Qty * stockData.InitQty2;

                    //        _context.SaveChanges();
                    //    }
                    //}

                    outData.EDITID = HttpContext.Session.GetString("UserAc");

                    //如果有公斤資料的話，轉換單位
                    if (outData.Kg != 0)
                    {

                        var kg = outData.Kg ?? 0;

                        outData.Kg = (double?)decimal.Round((decimal)kg / 1000, 2);
                    }
                    else
                    {
                        outData.Kg = 0;
                    }

                    //合計金額減掉免稅金額等於應稅金額
                    outData.Total1 = outData.Total - outData.Total0;
                    outData.Memo = vm.Memo;

                    _context.SaveChanges();

                    //清掉不必要的參數
                    string area = vm.Area;
                    string customerId = vm.CustomerId;
                    vm = new OutEditViewMidel
                    {
                        Area = area,
                        CustomerId = "",
                        OrderState = "4",
                    };

                    HttpContext.Session.SetString("msg", "修改成功");
                    return RedirectToAction("Index", "Out", vm);
                }
                return View(vm);
            }
            catch(Exception e)
            {
                throw;
            }

        }
        
        public async Task<IActionResult> Submit(string submitOrder)
        {
            if (!String.IsNullOrEmpty(submitOrder))
            {
                var OrderArray = submitOrder.Split(',');

                if(OrderArray.Length > 0)
                {
                    foreach(var order in OrderArray)
                    {
                        var orderdata = _context.Out10.Where(x => x.OutNo == order).FirstOrDefault();

                        if(orderdata != null)
                        {
                            orderdata.TallyState = "1";
                            orderdata.TALLYID = HttpContext.Session.GetString("UserAc");
                            await _context.SaveChangesAsync();
                        }
                    }
                }
            }
            return RedirectToAction("Index");
        }
    }
}
