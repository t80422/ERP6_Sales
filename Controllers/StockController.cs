using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP6.Models;
using X.PagedList;
using ERP6.ViewModels.Stock;

namespace ERP6.Controllers
{
    public class StockController : Controller
    {
        private readonly EEPEF01Context _context;

        public StockController(EEPEF01Context context)
        {
            _context = context;
        }

        /// <summary>
        /// 產品首頁
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index(StockIndexViewModel vm, int? page)
        {
            vm = new StockIndexViewModel();

            try
            {
                //TODO 取得產品資料
                var StockList = _context.Stock10.Select(x => new StockList
                {
                    PartNo = x.PartNo,
                    Barcode = x.Barcode,
                    Spec = x.Spec,
                    Unit = x.Unit,

                }).ToList();

                //TODO 搜尋條件(產品)

                vm.stockList = await StockList.ToPagedListAsync((int)page, 10);

                return View(vm);
            }
            catch (Exception e)
            {

                throw;
            }
        }

        /// <summary>
        /// 新增產品資料
        /// </summary>
        /// <returns></returns>
        public IActionResult Add()
        {
            try
            {
                StockAddViewModel vm = new StockAddViewModel();

                return View(vm);
            }
            catch (Exception e)
            {
                throw;
            }
        }

        /// <summary>
        /// 新增產品資料
        /// </summary>
        /// <param name="vm">StockAddViewModel</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(StockAddViewModel vm)
        {
            try
            {
                //TODO 新增所需資料(產品)
                Stock10 insertData = new Stock10();

                insertData.PartNo = vm.PartNo;
                insertData.Barcode = vm.Barcode;
                insertData.Spec = vm.Spec;
                insertData.Unit = vm.Unit;

                _context.Stock10.Add(insertData);
                await _context.SaveChangesAsync();

                return View(vm);
            }
            catch (Exception e)
            {
                throw;
            }
        }

        /// <summary>
        /// 編輯產品資料
        /// </summary>
        /// <param name="StockId">產品代碼</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(string StockId)
        {
            try
            {
                if (string.IsNullOrEmpty(StockId))
                {
                    return NotFound();
                }

                //TODO 編輯產品資料
                var updateData = await _context.Stock10.Select(x => new StockEditViewModel { }).FirstOrDefaultAsync();

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
        /// 編輯產品資料
        /// </summary>
        /// <param name="vm">StockEditViewModel</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(StockEditViewModel vm)
        {
            if (vm != null)
            {
                Stock10 updateData = new Stock10();

                updateData.PartNo = vm.PartNo;
                updateData.Barcode = vm.Barcode;
                updateData.Spec = vm.Spec;
                updateData.Unit = vm.Unit;

                _context.Stock10.Update(updateData);

                await _context.SaveChangesAsync();
            }

            return View(vm);
        }

        /// <summary>
        /// 刪除產品資料
        /// </summary>
        /// <param name="StockId">產品代碼</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(string StockId)
        {
            try
            {
                if (string.IsNullOrEmpty(StockId))
                {
                    return NotFound();
                }

                var deleteData = await _context.Stock10
                    .Where(x => x.PartNo == StockId).FirstOrDefaultAsync();

                if (deleteData != null)
                {
                    _context.Stock10.Remove(deleteData);
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
