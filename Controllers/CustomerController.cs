using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP6.Models;
using X.PagedList;
using ERP6.ViewModels.Customer;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ERP6.Controllers
{
    public class CustomerController : Controller
    {
        private readonly EEPEF01Context _context;

        public CustomerController(EEPEF01Context context)
        {
            _context = context;
        }

        /// <summary>
        /// 客戶首頁
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index(CustomerIndexViewModel vm)
        {
            try
            {

                #region SelectList

                //區域類別
                ViewBag.Area = new SelectList(_context.STO_AREA.Where(x => x.AREA_STATE == 1).ToList(), "AREA_NAME", "AREA_NAME", string.Empty);

                //業務人員
                ViewBag.BusinessList = new SelectList(_context.Pepo10.Where(x => x.Dep == "業務部").ToList(), "PeNo", "Name", string.Empty);

                //司機人員
                ViewBag.DriverList = new SelectList(_context.Pepo10.Where(x => x.Posi == "司機").ToList(), "PeNo", "Name", string.Empty);

                #endregion

                //取得客戶資料
                var CustomerList = await _context.Cstm10.Select(x => new CustomerList
                {
                    CoNo = x.CoNo,
                    Coname = x.Coname,
                    Company = x.Company,
                    Invocomp = x.Invocomp,
                    Dlien = x.Dlien,
                    Fax = x.Fax,
                    PrintPrice = x.PrintPrice,
                    Boss = x.Boss,
                    Sales = x.Sales,
                    Memo = x.Memo,
                    TaxType = x.TaxType,
                    Tel1 = x.Tel1,
                    Tel2 = x.Tel2,
                    Uniform = x.Uniform,
                    Compaddr = x.Compaddr,
                    Sendaddr = x.Sendaddr,
                    Payaccount = x.Payaccount,
                    Paybank = x.Paybank,
                    Email = x.Email,
                    Www = x.Www,
                    Prenotget = x.Prenotget,
                    Total1 = x.Total1,
                    Tax = x.Tax,
                    Total2 = x.Total2,
                    YesGet = x.YesGet,
                    SubTot = x.SubTot,
                    NotGet = x.NotGet,
                    PeNo = x.PeNo,
                    Product = x.Product,
                    AreaNo = x.AreaNo,
                    Payment = x.Payment,
                    ChehkDay = x.ChehkDay,
                    Ntus = x.Ntus,
                    Taxrate = x.Taxrate,
                    Mobile = x.Mobile,
                    Discount = x.Discount,
                    CusType = x.CusType,
                    PriceType = x.PriceType,
                    DriveNo = x.DriveNo
                }).ToListAsync();

                //TODO 搜尋條件(客戶)
                if (vm.IsSearch)
                {
                    //客戶編號
                    if (!string.IsNullOrEmpty(vm.CoNo))
                    {
                        CustomerList = await CustomerList.Where(x => x.CoNo.Contains(vm.CoNo)).ToListAsync();
                    }

                    //統一編號
                    if (!string.IsNullOrEmpty(vm.Uniform))
                    {
                        CustomerList = await CustomerList.Where(x => x.Uniform.Contains(vm.Uniform)).ToListAsync();
                    }

                    //客戶簡稱
                    if (!string.IsNullOrEmpty(vm.Coname))
                    {
                        CustomerList = await CustomerList.Where(x => x.Coname.Contains(vm.Coname)).ToListAsync();
                    }

                    //公司名稱
                    if (!string.IsNullOrEmpty(vm.Company))
                    {
                        CustomerList = await CustomerList.Where(x => x.Company.Contains(vm.Company)).ToListAsync();
                    }

                    //發票抬頭
                    if (!string.IsNullOrEmpty(vm.Invocomp))
                    {
                        CustomerList = await CustomerList.Where(x => x.Invocomp.Contains(vm.Invocomp)).ToListAsync();
                    }

                    //電話一
                    if (!string.IsNullOrEmpty(vm.Tel1))
                    {
                        CustomerList = await CustomerList.Where(x => x.Tel1.Contains(vm.Tel1)).ToListAsync();
                    }

                    //負責人
                    if (!string.IsNullOrEmpty(vm.Boss))
                    {
                        CustomerList = await CustomerList.Where(x => x.Boss.Contains(vm.Boss)).ToListAsync();
                    }

                    //傳真
                    if (!string.IsNullOrEmpty(vm.Fax))
                    {
                        CustomerList = await CustomerList.Where(x => x.Fax.Contains(vm.Fax)).ToListAsync();
                    }

                    //電話二
                    if (!string.IsNullOrEmpty(vm.Tel2))
                    {
                        CustomerList = await CustomerList.Where(x => x.Tel2.Contains(vm.Tel2)).ToListAsync();
                    }

                    //聯絡人
                    if (!string.IsNullOrEmpty(vm.Sales))
                    {
                        CustomerList = await CustomerList.Where(x => x.Sales.Contains(vm.Sales)).ToListAsync();
                    }

                    //行動電話
                    if (!string.IsNullOrEmpty(vm.Mobile))
                    {
                        CustomerList = await CustomerList.Where(x => x.Mobile.Contains(vm.Mobile)).ToListAsync();
                    }

                    //Email
                    if (!string.IsNullOrEmpty(vm.Email))
                    {
                        CustomerList = await CustomerList.Where(x => x.Email.Contains(vm.Email)).ToListAsync();
                    }
                }

                vm.customerList = CustomerList;

                return View(vm);
            }
            catch (Exception e)
            {

                throw;
            }
        }

        /// <summary>
        /// 新增客戶資料
        /// </summary>
        /// <returns></returns>
        public IActionResult Add()
        {
            try
            {
                CustomerAddViewModel vm = new CustomerAddViewModel();

                return View(vm);
            }
            catch (Exception e)
            {
                throw;
            }
        }

        /// <summary>
        /// 新增客戶資料
        /// </summary>
        /// <param name="vm">CustomerAddViewModel</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(CustomerAddViewModel vm)
        {
            try
            {
                //TODO 新增所需資料(客戶)
                Cstm10 insertData = new Cstm10();

                insertData.CoNo = vm.CoNo;
                insertData.Coname = vm.Coname;
                insertData.Company = vm.Company;
                insertData.Invocomp = vm.Invocomp;

                _context.Cstm10.Add(insertData);
                await _context.SaveChangesAsync();

                return View(vm);
            }
            catch (Exception e)
            {
                throw;
            }
        }

        /// <summary>
        /// 編輯客戶資料
        /// </summary>
        /// <param name="CustomerId">客戶代碼</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(string CustomerId)
        {
            try
            {
                if (string.IsNullOrEmpty(CustomerId))
                {
                    return NotFound();
                }

                //TODO 編輯客戶資料
                var updateData = await _context.Cstm10.Select(x => new CustomerEditViewModel { }).FirstOrDefaultAsync();

                if (updateData != null)
                {
                    return View(updateData);
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception e)
            {
                throw;
            }

        }

        /// <summary>
        /// 編輯客戶資料
        /// </summary>
        /// <param name="vm">CustomerEditViewModel</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CustomerEditViewModel vm)
        {
            if (vm != null)
            {
                Cstm10 updateData = new Cstm10();

                updateData.CoNo = vm.CoNo;
                updateData.Coname = vm.Coname;
                updateData.Company = vm.Company;
                updateData.Invocomp = vm.Invocomp;

                _context.Cstm10.Update(updateData);

                await _context.SaveChangesAsync();
            }

            return View(vm);
        }

        /// <summary>
        /// 刪除客戶資料
        /// </summary>
        /// <param name="CustomerId">客戶代碼</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(string CustomerId)
        {
            try
            {
                if (string.IsNullOrEmpty(CustomerId))
                {
                    return NotFound();
                }

                var deleteData = await _context.Cstm10
                    .Where(x => x.CoNo == CustomerId).FirstOrDefaultAsync();

                if (deleteData != null)
                {
                    _context.Cstm10.Remove(deleteData);
                    await _context.SaveChangesAsync();
                }

                return View();
            }
            catch (Exception e)
            {

                throw;
            }
        }
    }
}
