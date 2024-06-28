using ERP6.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERP6.ViewModels.Return;
using Microsoft.AspNetCore.Mvc.Rendering;
using X.PagedList;
using Microsoft.AspNetCore.Http;

namespace ERP6.Controllers
{
    public class ReturnController : Controller
    {
        private readonly EEPEF01Context _context;
        
        public ReturnController (EEPEF01Context context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(ReturnViewModel vm , int? page = 1)
        {
            #region SelectList

            // 退回單狀態
            ViewBag.StatesList = new SelectList(new List<SelectListItem>()
            {
                new SelectListItem{Text="全部退回單" , Value ="4"},
                new SelectListItem{Text="未入庫退回單",Value="2"},
                new SelectListItem{Text="已入庫退回單",Value="1"}
            }, "Value", "Text", vm.ReturnState);
            // 司機
            ViewBag.DriverList = new SelectList(_context.Pepo10.Where(x => x.Posi == "司機").ToList(), "PeNo", "Name", vm.DriverNo);
            // 業務
            ViewBag.PerNoList = new SelectList(_context.Pepo10.Where(x => x.Posi == "業務員").ToList(), "PeNo", "Name", vm.PerNo);
            // 區域
            ViewBag.Area = new SelectList(_context.STO_AREA.Where(x => x.AREA_STATE == 1).ToList(), "AREA_NAME", "AREA_NAME", vm.AreaNo);
            // 客戶名稱
            ViewBag.Client = new SelectList( !String.IsNullOrEmpty(vm.AreaNo) ? _context.Cstm10.Where(x=>x.AreaNo == vm.AreaNo).ToList() 
                                                : _context.Cstm10.ToList(), "CoNo", "Coname", string.Empty);

            #endregion SelectList

            var List = await _context.Out10.Where(x => x.OutType == "2").ToList().Join(_context.Cstm10, x=>x.CoNo ?? "" , y=>y.CoNo ?? "" ,(x,y)=> new {
                PASSED = x.PASSED ?? false,
                OutNo = x.OutNo ?? "",
                KeyinDate = !String.IsNullOrEmpty(x.KeyinDate) ? DateTime.ParseExact((x.KeyinDate).PadRight(12, '0'), "yyyyMMddHHmm", null).ToString("yyyy-MM-dd HH:mm") : "",
                ReturnDate = !String.IsNullOrEmpty(x.KeyinDate) ? DateTime.ParseExact((x.KeyinDate).PadRight(12, '0'), "yyyyMMddHHmm", null).ToString("yyyy-MM-dd") : "",
                DriveNo = x.DriveNo ?? "",
                PeNo = x.PeNo ?? "",
                AreaNo = y.AreaNo ?? "",
                CoNo = y.CoNo ?? "",
                //CoName = _context.Cstm10.Where(x => x.CoNo == (y.CoNo ?? "")).FirstOrDefault() != null ? (_context.Cstm10.Where(x => x.CoNo == (y.CoNo ?? "")).FirstOrDefault()?.Coname ?? "") : "",
                Total = x.Total ?? 0,
            }).ToListAsync();

            if(vm != null && vm.IsSearch)
            {
                // 入庫狀態
                if (!String.IsNullOrEmpty(vm.ReturnState) && vm.ReturnState != "4")
                {
                    List = await List.Where(x => x.PASSED == (vm.ReturnState == "1" ? true : false)).ToListAsync();
                }

                // 訂單編號
                if (!String.IsNullOrEmpty(vm.ReturnNo))
                {
                    List = await List.Where(x => x.OutNo.Contains(vm.ReturnNo)).ToListAsync();
                }

                // 訂購時間
                if (!String.IsNullOrEmpty(vm.ReturnTime))
                {
                    List = await List.Where(x => x.ReturnDate == vm.ReturnTime).ToListAsync();
                }
                // 司機
                if (!String.IsNullOrEmpty(vm.DriverNo))
                {
                    List = await List.Where(x => x.DriveNo == vm.DriverNo).ToListAsync();
                }
                // 業務
                if (!String.IsNullOrEmpty(vm.PerNo))
                {
                    List = await List.Where(x => x.PeNo == vm.PerNo).ToListAsync();
                }
                // 區域
                if (!String.IsNullOrEmpty(vm.AreaNo))
                {
                    List = await List.Where(x => x.AreaNo == vm.AreaNo).ToListAsync();
                }
                // 客戶名稱
                if (!String.IsNullOrEmpty(vm.CoNo))
                {
                    List = await List.Where(x => x.CoNo == vm.CoNo).ToListAsync();
                }
            }
            //else
            //{
            //    var Today = DateTime.Now.ToString("yyyy-MM-dd");
            //    List = await List.Where(x => x.KeyinDate.StartsWith(Today)).ToListAsync();
            //}

            //List = !vm.IsSearch ? await List.Take(100).ToListAsync() : List;
            vm.AllPassed = !List.Where(x=>!x.PASSED).Any();
            vm.Passed = List.Where(x => x.PASSED).Count();
            vm.NotPassed = List.Where(x => !x.PASSED).Count();


            vm.ReturnList = await List.Select(x => new ReturnList
            {
                ReturnState = x.PASSED,
                ReturnNo = x.OutNo,
                ReturnTime = x.KeyinDate,
                Amount = x.Total,
                CoName = _context.Cstm10.Find(x.CoNo)?.Coname ?? "",
            }).OrderByDescending(x=>x.ReturnNo).ToPagedListAsync((int)page, 10);


            return View(vm);
        }

        public async Task<IActionResult> Submit(string submitOrder)
        {
            if (!String.IsNullOrEmpty(submitOrder))
            {
                var OrderArray = submitOrder.Split(',');

                if (OrderArray.Length > 0)
                {
                    foreach (var order in OrderArray)
                    {
                        var orderdata = _context.Out10.Where(x => x.OutNo == order).FirstOrDefault();

                        if (orderdata != null)
                        {
                            orderdata.PASSED = true;
                            orderdata.PASSEDID = HttpContext.Session.GetString("UserAc") ?? "";
                            await _context.SaveChangesAsync();
                        }
                    }
                }
            }
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> Add(string CoNo)
        {
            CoNo = CoNo ?? "";
            #region SelectList
            // 客戶
            ViewBag.Client = new SelectList(await _context.Cstm10.ToListAsync(), "CoNo", "Coname", CoNo);

            #endregion SelectList

            ReturnAddViewModel vm = new ReturnAddViewModel();
            vm.CoNo = CoNo;
            vm.PASSED = false;
            vm.ReturnTime = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");            
            vm.UserId = HttpContext.Session.GetString("UserAc");
            vm.EditId = HttpContext.Session.GetString("UserAc");
            vm.PassedId = "";

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(
            ReturnAddViewModel vm , List<string> PartNo , List<string> Price ,List<string> Discount, List<string> Amount , List<string> BAmount, List<string> EAmount, List<string> PAmount, List<string> OAmount, List<string> PMemo)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var CSTM = _context.Cstm10.Find(vm.CoNo ?? "");

                    #region 計算資料

                    // 客戶折扣
                    double DISCOUNT = CSTM != null ? (double)(CSTM?.Discount ?? 0) : 0;
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

                    #endregion 計算資料

                    // 找出這個月最新的單號
                    var cond = DateTime.Now.ToString("yyyMM");
                    var lastOutNo = _context.Out10.Where(x => x.OutNo.Contains(cond) && x.OutType == "2").OrderByDescending(x => x.OutNo).FirstOrDefault()?.OutNo;
                    // 如果有最新單號
                    vm.ReturnNo = !String.IsNullOrEmpty(lastOutNo) ? (int.Parse(lastOutNo) + 1).ToString() : cond + "8001";

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
                        OutNo = vm.ReturnNo,
                        OutType = "2", // 預設 2:出貨退回
                        OutDate = vm.ReturnTime.Replace("-",""), // 出貨日期為建檔+1天
                        Paymonth = PayMonth, // 客戶結帳日前為當月，後為下月
                        Ntus = "NTD",
                        CoNo = vm.CoNo ?? "",
                        Memo = vm.Memo ?? "",
                        Total1 = TOTAL1,
                        Userid = HttpContext.Session.GetString("UserAc") ?? "",
                        StockPass = null,
                        YnClose = null,
                        PeNo = CSTM != null ? (CSTM.PeNo ?? "") : "",
                        KeyinDate = DateTime.Now.ToString("yyyyMMddHHmm"),
                        YesGet = YESGET, // 預設為0
                        CashDis = CASHDIS,
                        SubTot = SUBTOT, // 預設為0
                        NotGet = NOTGET,
                        Total0 = TOTAL0,
                        Total = TOTAL,
                        DriveNo = CSTM != null ? (CSTM.DriveNo ?? "") : "",
                        Discount = 0,
                        Tax = 0, // 預設
                        Kg = KG,
                        Promotion_No = null,
                        TallyState = "2", // 預設為未理貨
                        EDITID = HttpContext.Session.GetString("UserAc") ?? "",
                    };

                    var NEWOUT10DATA = _context.Out10.Add(insertData);
                    Out10 INSERTDATA = NEWOUT10DATA.Entity;
                    await _context.SaveChangesAsync();

                    // 加入明細資料
                    int i = 0;
                    List<Out20> Out20InsertData = new List<Out20>();
                    // 上筆訂單的產品
                    var LAST_OUTLIST = _context.Out10.Where(x => x.CoNo == vm.CoNo && String.Compare(x.OutNo, vm.ReturnNo) <= 0 && x.OutNo != vm.ReturnNo && x.OutType == "1");

                    var LAST_STOCK = LAST_OUTLIST != null ? _context.Out20.Join(LAST_OUTLIST, x => x.OutNo, y => y.OutNo, (x, y) => new { PartNo = x.PartNo, Qty = x.Qty })
                        .Where(x => x.Qty > 0).Select(x => x.PartNo).Distinct().ToList() : new List<string>();


                    foreach (var partno in PartNo){
                        if(Convert.ToInt32(BAmount[i]) > 0 || Convert.ToInt32(EAmount[i]) > 0 || Convert.ToInt32(PAmount[i]) > 0 || Convert.ToInt32(OAmount[i]) > 0)
                        {
                            // 序號
                            int SERNO = i + 1;
                            // 取得產品資料
                            var STOCKDATA = _context.Stock10.Find(partno);
                            // 數量
                            double STOCK_QTY = Convert.ToDouble(BAmount[i]) + Convert.ToDouble(EAmount[i]) + Convert.ToDouble(PAmount[i]) + Convert.ToDouble(OAmount[i]);
                            // 價格
                            double STOCK_PRICE = Convert.ToDouble(Price[i]);
                            // 商品折扣
                            double STOCK_DIS = Convert.ToDouble(Discount[i]);
                            // 總計
                            double STOCK_AMOUNT = Math.Round((STOCK_PRICE * STOCK_QTY) * (100 - STOCK_DIS) / 100, 2);
                            // 建議售價
                            double? STOCK_SP = 0;
                            // 牌價
                            double? STOCK_PP = 0;

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

                            Out20InsertData.Add(new Out20()
                            {
                                OutNo = vm.ReturnNo ?? "",
                                Serno = SERNO,
                                PartNo = partno ?? "",
                                Qty = STOCK_QTY,
                                Price = STOCK_PRICE,
                                Amount = STOCK_AMOUNT,
                                Discount = STOCK_DIS,
                                Unit = STOCKDATA != null ? STOCKDATA.Unit : "",
                                Memo = PMemo[i] ?? "",
                                SPrice = STOCK_SP,
                                PPrice = STOCK_PP,
                                IsPromise = LAST_STOCK.Contains(partno) ? "1" : null,
                                BROKEN = Convert.ToInt32(BAmount[i] ?? "0"),
                                EXPIRED = Convert.ToInt32(EAmount[i] ?? "0"),
                                PERFECT = Convert.ToInt32(PAmount[i] ?? "0"),
                                OTHER = Convert.ToInt32(OAmount[i] ?? "0"),

                            });

                            // 計算總金額
                            if (STOCKDATA.TaxType == "免稅") { TOTAL0 += Math.Round(STOCK_AMOUNT, 2); }
                            if (STOCKDATA.TaxType == "應稅") { TOTAL1 += Math.Round(STOCK_AMOUNT, 2); }
                            TOTAL += Math.Round(STOCK_AMOUNT, 2);

                            KG += Convert.ToDouble(STOCK_QTY * STOCKDATA.InitQty2);
                        }
                        i++;
                    }
                    _context.Out20.AddRange(Out20InsertData);
                    await _context.SaveChangesAsync();

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

                    ReturnViewModel Newvm = new ReturnViewModel()
                    {
                        CoNo = vm.CoNo ,
                        ReturnState = "4",
                        ReturnTime = DateTime.Today.ToString("yyyy-MM-dd"),
                        IsSearch = true,
                    };
                    return RedirectToAction("Index", Newvm);
                }

                ViewBag.Client = new SelectList(await _context.Cstm10.ToListAsync(), "CoNo", "Coname", string.Empty);
                return View(vm);
            }
            catch(Exception e)
            {
                throw;
            }
        }

        public async Task<IActionResult> Edit(string ReturnNo)
        {
            if (String.IsNullOrEmpty(ReturnNo)) { return NotFound(); }            

            var ReturnData = _context.Out10.Find(ReturnNo);

            if(ReturnData == null) { return NotFound(); }

            #region SelectList
            // 客戶
            ViewBag.Client = new SelectList(await _context.Cstm10.ToListAsync(), "CoNo", "Coname", string.Empty);

            #endregion SelectList

            ReturnAddViewModel vm = new ReturnAddViewModel();

            double TotalQty = 0;
            double TotalAmount = 0;

            vm.ReturnNo = ReturnNo;
            vm.PASSED = ReturnData.PASSED ?? false;
            vm.EditId = HttpContext.Session.GetString("UserAc");
            vm.UserId = ReturnData.Userid;
            vm.Memo = ReturnData.Memo ?? "";
            vm.CoNo = ReturnData.CoNo ?? "";
            vm.PassedId = ReturnData.PASSEDID ?? "";
            vm.ReturnTime = !String.IsNullOrEmpty(ReturnData.OutDate) ? DateTime.ParseExact(ReturnData.OutDate, "yyyyMMdd", null).ToString("yyyy-MM-dd") : "";
            vm.StockList = _context.Out20.Where(x => x.OutNo == ReturnNo).OrderBy(x => x.Serno).Select(x=>new StockList {
                PartNo = x.PartNo ?? "", 
                PartName = "",
                Price = x.Price ?? 0,
                Amount = Convert.ToInt32(x.Qty ?? 0),
                BAmount = x.BROKEN ?? 0,
                EAmount = x.EXPIRED ?? 0,
                PAmount = x.PERFECT ?? 0,
                OAmount = x.OTHER ?? 0 ,
                PMemo = x.Memo ?? "",
                Discount = x.Discount ?? 0,
            }).ToList();

            TotalAmount = Convert.ToDouble(ReturnData.Total ?? 0);

            Stock10 stock = new Stock10();

            foreach(var it in vm.StockList)
            {
                stock = _context.Stock10.Find(it.PartNo);
                it.PartName = stock != null ? (it.PartNo + stock.Spec + stock.Barcode) : it.PartNo;
                TotalQty += Convert.ToDouble(it.Amount);
            }

            ViewBag.Qty = TotalQty;
            ViewBag.Amount = TotalAmount;

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
    ReturnAddViewModel vm, List<string> PartNo, List<string> Price, List<string> Amount, List<string> Discount, List<string> BAmount, List<string> EAmount, List<string> PAmount, List<string> OAmount, List<string> PMemo)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var CSTM = _context.Cstm10.Find(vm.CoNo ?? "");

                    #region 計算資料

                    // 客戶折扣
                    double DISCOUNT = CSTM != null ? (double)(CSTM?.Discount ?? 0) : 0;
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

                    #endregion 計算資料

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

                    var INSERTDATA = _context.Out10.Find(vm.ReturnNo);

                    INSERTDATA.CoNo = vm.CoNo ?? "";
                    INSERTDATA.Paymonth = PayMonth;
                    INSERTDATA.Memo = vm.Memo ?? "";
                    INSERTDATA.Total1 = TOTAL1;
                    INSERTDATA.YesGet = YESGET;
                    INSERTDATA.CashDis = CASHDIS;
                    INSERTDATA.SubTot = SUBTOT;
                    INSERTDATA.NotGet = NOTGET;
                    INSERTDATA.Total0 = TOTAL0;
                    INSERTDATA.Total = TOTAL;
                    INSERTDATA.Kg = KG;
                    INSERTDATA.OutDate = vm.ReturnTime.Replace("-", "");
                    INSERTDATA.EDITID = HttpContext.Session.GetString("UserAc") ?? "";

                    _context.Out20.RemoveRange(_context.Out20.Where(x => x.OutNo == vm.ReturnNo).ToList());
                    await _context.SaveChangesAsync();


                    // 加入明細資料
                    int i = 0;
                    List<Out20> Out20InsertData = new List<Out20>();
                    // 上筆訂單的產品
                    var LAST_OUTLIST = _context.Out10.Where(x => x.CoNo == vm.CoNo && String.Compare(x.OutNo, vm.ReturnNo) <= 0 && x.OutNo != vm.ReturnNo && x.OutType == "1");

                    var LAST_STOCK = LAST_OUTLIST != null ? _context.Out20.Join(LAST_OUTLIST, x => x.OutNo, y => y.OutNo, (x, y) => new { PartNo = x.PartNo, Qty = x.Qty })
                        .Where(x => x.Qty > 0).Select(x => x.PartNo).Distinct().ToList() : new List<string>();


                    foreach (var partno in PartNo)
                    {
                        if (Convert.ToInt32(BAmount[i]) > 0 || Convert.ToInt32(EAmount[i]) > 0 || Convert.ToInt32(PAmount[i]) > 0 || Convert.ToInt32(OAmount[i]) > 0)
                        {
                            // 序號
                            int SERNO = i + 1;
                            // 取得產品資料
                            var STOCKDATA = _context.Stock10.Find(partno);
                            // 數量
                            double STOCK_QTY = Convert.ToDouble(BAmount[i]) + Convert.ToDouble(EAmount[i]) + Convert.ToDouble(PAmount[i]) + Convert.ToDouble(OAmount[i]);
                            // 價格
                            double STOCK_PRICE = Convert.ToDouble(Price[i]);
                            // 商品折扣
                            double STOCK_DIS = Convert.ToDouble(Discount[i]);
                            // 總計
                            double STOCK_AMOUNT = Math.Round((STOCK_PRICE * STOCK_QTY) * (100 - STOCK_DIS) / 100, 2);
                            // 建議售價
                            double? STOCK_SP = 0;
                            // 牌價
                            double? STOCK_PP = 0;

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

                            Out20InsertData.Add(new Out20()
                            {
                                OutNo = vm.ReturnNo ?? "",
                                Serno = SERNO,
                                PartNo = partno ?? "",
                                Qty = STOCK_QTY,
                                Price = STOCK_PRICE,
                                Amount = STOCK_AMOUNT,
                                Discount = STOCK_DIS,
                                Unit = STOCKDATA != null ? STOCKDATA.Unit : "",
                                Memo = PMemo[i] ?? "",
                                SPrice = STOCK_SP,
                                PPrice = STOCK_PP,
                                IsPromise = LAST_STOCK.Contains(partno) ? "1" : null,
                                BROKEN = Convert.ToInt32(BAmount[i] ?? "0"),
                                EXPIRED = Convert.ToInt32(EAmount[i] ?? "0"),
                                PERFECT = Convert.ToInt32(PAmount[i] ?? "0"),
                                OTHER = Convert.ToInt32(OAmount[i] ?? "0"),

                            });

                            // 計算總金額
                            if (STOCKDATA.TaxType == "免稅") { TOTAL0 += Math.Round(STOCK_AMOUNT, 2); }
                            if (STOCKDATA.TaxType == "應稅") { TOTAL1 += Math.Round(STOCK_AMOUNT, 2); }
                            TOTAL += Math.Round(STOCK_AMOUNT, 2);

                            KG += Convert.ToDouble(STOCK_QTY * STOCKDATA.InitQty2);
                        }
                        i++;
                    }
                    _context.Out20.AddRange(Out20InsertData);
                    await _context.SaveChangesAsync();

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
                    HttpContext.Session.SetString("msg", "修改成功");

                    #endregion 計算異動資料

                    ReturnViewModel Newvm = new ReturnViewModel()
                    {
                        CoNo = vm.CoNo,
                        ReturnState = "4",
                        IsSearch = true,
                    };
                    return RedirectToAction("Index", Newvm);
                }

                ViewBag.Client = new SelectList(await _context.Cstm10.ToListAsync(), "CoNo", "Coname", string.Empty);
                return View(vm);
            }
            catch (Exception e)
            {
                throw;
            }
        }

    }
}
