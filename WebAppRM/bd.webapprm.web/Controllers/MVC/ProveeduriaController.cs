using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using bd.webapprm.servicios.Interfaces;
using bd.webapprm.entidades.Utils;
using bd.log.guardar.Servicios;
using bd.log.guardar.ObjectTranfer;
using bd.log.guardar.Enumeradores;
using Newtonsoft.Json;
using bd.webapprm.entidades;
using bbd.webapprm.servicios.Enumeradores;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace bd.webapprm.web.Controllers.MVC
{
    public class ProveeduriaController : Controller
    {
        private readonly IApiServicio apiServicio;

        public static List<Factura> ListadoFacturasSeleccionadas = new List<Factura>();

        public static List<Factura> ListadoFacturas = new List<Factura>();

        public ProveeduriaController(IApiServicio apiServicio)
        {
            this.apiServicio = apiServicio;
        }

        #region Recepci�n de Proveedur�a
        public IActionResult Index()
        {
            return RedirectToAction("RecepcionProveeduria");
        }

        public async Task<IActionResult> RecepcionProveeduria()
        {
            ViewData["TipoArticulo"] = new SelectList(await apiServicio.Listar<TipoArticulo>(new Uri(WebApp.BaseAddress), "/api/TipoArticulo/ListarTipoArticulo"), "IdTipoArticulo", "Nombre");
            ViewData["ClaseArticulo"] = await ObtenerSelectListClaseArticulo((ViewData["TipoArticulo"] as SelectList).FirstOrDefault() != null ? int.Parse((ViewData["TipoArticulo"] as SelectList).FirstOrDefault().Value) : -1);
            ViewData["SubClaseArticulo"] = await ObtenerSelectListSubClaseArticulo((ViewData["ClaseArticulo"] as SelectList).FirstOrDefault() != null ? int.Parse((ViewData["ClaseArticulo"] as SelectList).FirstOrDefault().Value) : -1);
            ViewData["Articulo"] = await ObtenerSelectListArticulo((ViewData["SubClaseArticulo"] as SelectList).FirstOrDefault() != null ? int.Parse((ViewData["SubClaseArticulo"] as SelectList).FirstOrDefault().Value) : -1);

            ViewData["Pais"] = new SelectList(await apiServicio.Listar<Pais>(new Uri(WebApp.BaseAddress), "/api/Pais/ListarPaises"), "IdPais", "Nombre");
            ViewData["Provincia"] = await ObtenerSelectListProvincia((ViewData["Pais"] as SelectList).FirstOrDefault() != null ? int.Parse((ViewData["Pais"] as SelectList).FirstOrDefault().Value) : -1);
            ViewData["Ciudad"] = await ObtenerSelectListCiudad((ViewData["Provincia"] as SelectList).FirstOrDefault() != null ? int.Parse((ViewData["Provincia"] as SelectList).FirstOrDefault().Value) : -1);
            ViewData["Sucursal"] = await ObtenerSelectListSucursal((ViewData["Ciudad"] as SelectList).FirstOrDefault() != null ? int.Parse((ViewData["Ciudad"] as SelectList).FirstOrDefault().Value) : -1);
            ViewData["MaestroArticuloSucursal"] = await ObtenerSelectListMaestroArticuloSucursal((ViewData["Sucursal"] as SelectList).FirstOrDefault() != null ? int.Parse((ViewData["Sucursal"] as SelectList).FirstOrDefault().Value) : -1);
            
            var listaProveedor = await apiServicio.Listar<Proveedor>(new Uri(WebApp.BaseAddress), "/api/Proveedor/ListarProveedores");
            var tlistaProveedor = listaProveedor.Select(c => new { IdProveedor = c.IdProveedor, NombreApellidos = String.Format("{0} {1}", c.Nombre, c.Apellidos) });
            ViewData["Proveedor"] = new SelectList(tlistaProveedor, "IdProveedor", "NombreApellidos");

            var listaEmpleado = await apiServicio.Listar<Empleado>(new Uri(WebApp.BaseAddress), "/api/Empleado/ListarEmpleados");
            var tlistaEmpleado = listaEmpleado.Select(c => new { IdEmpleado = c.IdEmpleado, NombreApellidos = String.Format("{0} {1}", c.Persona.Nombres, c.Persona.Apellidos) });
            ViewData["Empleado"] = new SelectList(tlistaEmpleado, "IdEmpleado", "NombreApellidos");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecepcionProveeduria(RecepcionArticulos recepcionArticulo)
        {
            Response response = new Response();
            try
            {
                if (recepcionArticulo.IdArticulo == 0)
                {
                    try
                    {
                        recepcionArticulo.Articulo = new Articulo();
                        int IdSubClaseArticulo = int.Parse(Request.Form["Articulo.IdSubClaseArticulo"].ToString());
                        var listaSubClaseArticulo = await apiServicio.Listar<SubClaseArticulo>(new Uri(WebApp.BaseAddress), "/api/SubClaseArticulo/ListarSubClaseArticulos");
                        recepcionArticulo.Articulo.SubClaseArticulo = listaSubClaseArticulo.SingleOrDefault(c => c.IdSubClaseArticulo == IdSubClaseArticulo);
                        recepcionArticulo.Articulo.IdSubClaseArticulo = IdSubClaseArticulo;
                    }
                    catch (Exception)
                    {
                        try
                        {
                            recepcionArticulo.Articulo.SubClaseArticulo = new SubClaseArticulo();
                            int IdClaseArticulo = int.Parse(Request.Form["Articulo.SubClaseArticulo.IdClaseArticulo"].ToString());
                            var listaClaseArticulo = await apiServicio.Listar<ClaseArticulo>(new Uri(WebApp.BaseAddress), "/api/ClaseArticulo/ListarClaseArticulo");
                            recepcionArticulo.Articulo.SubClaseArticulo.ClaseArticulo = listaClaseArticulo.SingleOrDefault(c => c.IdClaseArticulo == IdClaseArticulo);
                            recepcionArticulo.Articulo.SubClaseArticulo.IdClaseArticulo = IdClaseArticulo;
                        }
                        catch (Exception)
                        {
                            try
                            {
                                recepcionArticulo.Articulo.SubClaseArticulo.ClaseArticulo = new ClaseArticulo();
                                int IdTipoArticulo = int.Parse(Request.Form["Articulo.SubClaseArticulo.ClaseArticulo.IdTipoArticulo"].ToString());
                                var listaTipoArticulo = await apiServicio.Listar<TipoArticulo>(new Uri(WebApp.BaseAddress), "/api/TipoArticulo/ListarTipoArticulo");
                                recepcionArticulo.Articulo.SubClaseArticulo.ClaseArticulo.TipoArticulo = listaTipoArticulo.SingleOrDefault(c => c.IdTipoArticulo == IdTipoArticulo);
                                recepcionArticulo.Articulo.SubClaseArticulo.ClaseArticulo.IdTipoArticulo = IdTipoArticulo;
                            }
                            catch (Exception)
                            { }
                        }
                    }
                }
                else
                {
                    var listaArticulo = await apiServicio.Listar<Articulo>(new Uri(WebApp.BaseAddress), "/api/Articulo/ListarArticulos");
                    recepcionArticulo.Articulo = listaArticulo.SingleOrDefault(c => c.IdArticulo == recepcionArticulo.IdArticulo);
                }

                if (recepcionArticulo.IdMaestroArticuloSucursal == 0)
                {
                    try
                    {
                        recepcionArticulo.MaestroArticuloSucursal = new MaestroArticuloSucursal();
                        int IdSucursal = int.Parse(Request.Form["MaestroArticuloSucursal.IdSucursal"].ToString());

                        var listaSucursal = await apiServicio.Listar<Sucursal>(new Uri(WebApp.BaseAddress), "/api/Sucursal/ListarSucursales");
                        recepcionArticulo.MaestroArticuloSucursal.Sucursal = listaSucursal.SingleOrDefault(c => c.IdSucursal == IdSucursal);
                        recepcionArticulo.MaestroArticuloSucursal.IdSucursal = IdSucursal;
                    }
                    catch (Exception)
                    {
                        try
                        {
                            recepcionArticulo.MaestroArticuloSucursal.Sucursal = new Sucursal();
                            int IdCiudad = int.Parse(Request.Form["MaestroArticuloSucursal.Sucursal.IdCiudad"].ToString());

                            var listaCiudad = await apiServicio.Listar<Ciudad>(new Uri(WebApp.BaseAddress), "/api/Ciudad/ListarCiudades");
                            recepcionArticulo.MaestroArticuloSucursal.Sucursal.Ciudad = listaCiudad.SingleOrDefault(c => c.IdCiudad == IdCiudad);
                            recepcionArticulo.MaestroArticuloSucursal.Sucursal.IdCiudad = IdCiudad;
                        }
                        catch (Exception)
                        {
                            try
                            {
                                recepcionArticulo.MaestroArticuloSucursal.Sucursal.Ciudad = new Ciudad();
                                int IdProvincia = int.Parse(Request.Form["MaestroArticuloSucursal.Sucursal.Ciudad.IdProvincia"].ToString());

                                var listaProvincia = await apiServicio.Listar<Provincia>(new Uri(WebApp.BaseAddress), "/api/Provincia/ListarProvincias");
                                recepcionArticulo.MaestroArticuloSucursal.Sucursal.Ciudad.Provincia = listaProvincia.SingleOrDefault(c => c.IdProvincia == IdProvincia);
                                recepcionArticulo.MaestroArticuloSucursal.Sucursal.Ciudad.IdProvincia = IdProvincia;
                            }
                            catch (Exception)
                            {
                                try
                                {
                                    recepcionArticulo.MaestroArticuloSucursal.Sucursal.Ciudad.Provincia = new Provincia();
                                    int IdPais = int.Parse(Request.Form["MaestroArticuloSucursal.Sucursal.Ciudad.Provincia.IdPais"].ToString());

                                    var listaPais = await apiServicio.Listar<Pais>(new Uri(WebApp.BaseAddress), "/api/Pais/ListarPaises");
                                    recepcionArticulo.MaestroArticuloSucursal.Sucursal.Ciudad.Provincia.Pais = listaPais.SingleOrDefault(c => c.IdPais == IdPais);
                                    recepcionArticulo.MaestroArticuloSucursal.Sucursal.Ciudad.Provincia.IdPais = IdPais;
                                }
                                catch (Exception)
                                { }
                            }
                        }
                    }
                }
                else
                {
                    var listaMaestroSucursal = await apiServicio.Listar<MaestroArticuloSucursal>(new Uri(WebApp.BaseAddress), "/api/MaestroArticuloSucursal/ListarMaestroArticuloSucursal");
                    recepcionArticulo.MaestroArticuloSucursal = listaMaestroSucursal.SingleOrDefault(c => c.IdMaestroArticuloSucursal == recepcionArticulo.IdMaestroArticuloSucursal);
                }
                TryValidateModel(recepcionArticulo);

                response = await apiServicio.InsertarAsync(recepcionArticulo,
                                                             new Uri(WebApp.BaseAddress),
                                                             "/api/RecepcionArticulo/InsertarRecepcionArticulo");

                if (response.IsSuccess)
                {
                    var responseLog = await GuardarLogService.SaveLogEntry(new LogEntryTranfer
                    {
                        ApplicationName = Convert.ToString(Aplicacion.WebAppRM),
                        ExceptionTrace = null,
                        Message = "Se ha recepcionado un art�culo",
                        UserName = "Usuario 1",
                        LogCategoryParametre = Convert.ToString(LogCategoryParameter.Create),
                        LogLevelShortName = Convert.ToString(LogLevelParameter.ADV),
                        EntityID = string.Format("{0} {1}", "Art�culo:", recepcionArticulo.IdArticulo),
                    });
                    return RedirectToAction("ProveeduriaRecepcionados");
                }

                ViewData["TipoArticulo"] = new SelectList(await apiServicio.Listar<TipoArticulo>(new Uri(WebApp.BaseAddress), "/api/TipoArticulo/ListarTipoArticulo"), "IdTipoArticulo", "Nombre");
                ViewData["ClaseArticulo"] = await ObtenerSelectListClaseArticulo(recepcionArticulo?.Articulo?.SubClaseArticulo?.ClaseArticulo?.IdTipoArticulo ?? -1);
                ViewData["SubClaseArticulo"] = await ObtenerSelectListSubClaseArticulo(recepcionArticulo?.Articulo?.SubClaseArticulo?.ClaseArticulo?.IdClaseArticulo ?? -1);
                ViewData["Articulo"] = await ObtenerSelectListArticulo(recepcionArticulo?.Articulo?.SubClaseArticulo?.IdSubClaseArticulo ?? -1);

                ViewData["Pais"] = new SelectList(await apiServicio.Listar<Pais>(new Uri(WebApp.BaseAddress), "/api/Pais/ListarPaises"), "IdPais", "Nombre");
                ViewData["Provincia"] = await ObtenerSelectListProvincia(recepcionArticulo?.MaestroArticuloSucursal?.Sucursal?.Ciudad?.Provincia?.Pais?.IdPais ?? -1);
                ViewData["Ciudad"] = await ObtenerSelectListCiudad(recepcionArticulo?.MaestroArticuloSucursal?.Sucursal?.Ciudad?.Provincia?.IdProvincia ?? -1);
                ViewData["Sucursal"] = await ObtenerSelectListSucursal(recepcionArticulo?.MaestroArticuloSucursal?.Sucursal?.Ciudad?.IdCiudad ?? -1);
                ViewData["MaestroArticuloSucursal"] = await ObtenerSelectListMaestroArticuloSucursal(recepcionArticulo?.MaestroArticuloSucursal?.Sucursal?.IdSucursal ?? -1);

                var listaProveedor = await apiServicio.Listar<Proveedor>(new Uri(WebApp.BaseAddress), "/api/Proveedor/ListarProveedores");
                var tlistaProveedor = listaProveedor.Select(c => new { IdProveedor = c.IdProveedor, NombreApellidos = String.Format("{0} {1}", c.Nombre, c.Apellidos) });
                ViewData["Proveedor"] = new SelectList(tlistaProveedor, "IdProveedor", "NombreApellidos");

                var listaEmpleado = await apiServicio.Listar<Empleado>(new Uri(WebApp.BaseAddress), "/api/Empleado/ListarEmpleados");
                var tlistaEmpleado = listaEmpleado.Select(c => new { IdEmpleado = c.IdEmpleado, NombreApellidos = String.Format("{0} {1}", c.Persona.Nombres, c.Persona.Apellidos) });
                ViewData["Empleado"] = new SelectList(tlistaEmpleado, "IdEmpleado", "NombreApellidos");

                ViewData["Error"] = response.Message;
                return View(recepcionArticulo);
            }
            catch (Exception ex)
            {
                await GuardarLogService.SaveLogEntry(new LogEntryTranfer
                {
                    ApplicationName = Convert.ToString(Aplicacion.WebAppRM),
                    Message = "Creando recepci�n Art�culo",
                    ExceptionTrace = ex,
                    LogCategoryParametre = Convert.ToString(LogCategoryParameter.Create),
                    LogLevelShortName = Convert.ToString(LogLevelParameter.ERR),
                    UserName = "Usuario APP WebAppTh"
                });
                return BadRequest();
            }
        }
        #endregion

        #region Alta de Proveedur�a
        public async Task<IActionResult> ArticulosADarAlta()
        {
            var lista = new List<RecepcionArticulos>();
            try
            {
                lista = await apiServicio.Listar<RecepcionArticulos>(new Uri(WebApp.BaseAddress)
                                                                    , "/api/RecepcionArticulo/ListarRecepcionArticulos");

                var listaArticulosRecepcionados = lista.Select(c=>c).ToList();

                return View("ArticulosAlta", listaArticulosRecepcionados);
            }
            catch (Exception ex)
            {
                await GuardarLogService.SaveLogEntry(new LogEntryTranfer
                {
                    ApplicationName = Convert.ToString(Aplicacion.WebAppRM),
                    Message = "Listando articulos recepcionados",
                    ExceptionTrace = ex,
                    LogCategoryParametre = Convert.ToString(LogCategoryParameter.NetActivity),
                    LogLevelShortName = Convert.ToString(LogLevelParameter.ERR),
                    UserName = "Usuario APP webappth"
                });
                return BadRequest();
            }
        }

        public async Task<IActionResult> FormularioAltaArticulo(int ID)
        {
            try
            {
                var respuesta = await apiServicio.SeleccionarAsync<Response>(ID.ToString(), new Uri(WebApp.BaseAddress), "/api/RecepcionArticulo");

                respuesta.Resultado = JsonConvert.DeserializeObject<RecepcionArticulos>(respuesta.Resultado.ToString());

                RecepcionArticulos RecepcionArticulos = respuesta.Resultado as RecepcionArticulos;

                ViewBag.Acreditacion = new SelectList(new List<string> { "Facturas", "Documentos" });
                
                if (respuesta.IsSuccess)
                {
                    try
                    {
                        return View("FormularioAltaArticulo", RecepcionArticulos);
                    }
                    catch (Exception ex)
                    {
                        await GuardarLogService.SaveLogEntry(new LogEntryTranfer
                        {
                            ApplicationName = Convert.ToString(Aplicacion.WebAppRM),
                            Message = "Listando facturas",
                            ExceptionTrace = ex,
                            LogCategoryParametre = Convert.ToString(LogCategoryParameter.Create),
                            LogLevelShortName = Convert.ToString(LogLevelParameter.ERR),
                            UserName = "Usuario APP WebAppTh"
                        });

                        return BadRequest();
                    }
                }
            }
            catch (Exception ex)
            {
                await GuardarLogService.SaveLogEntry(new LogEntryTranfer
                {
                    ApplicationName = Convert.ToString(Aplicacion.WebAppRM),
                    Message = "Listando un objeto de RecepcionArticulos",
                    ExceptionTrace = ex,
                    LogCategoryParametre = Convert.ToString(LogCategoryParameter.Create),
                    LogLevelShortName = Convert.ToString(LogLevelParameter.ERR),
                    UserName = "Usuario APP WebAppTh"
                });

                return BadRequest();
            }

            return View();            
        }

        public async Task<IActionResult> CargarTablaFacturasExcluidas(int ID)
        {
            try
            {
                var respuesta = await apiServicio.SeleccionarAsync<Response>(ID.ToString(), new Uri(WebApp.BaseAddress), "/api/RecepcionArticulo");

                respuesta.Resultado = JsonConvert.DeserializeObject<RecepcionArticulos>(respuesta.Resultado.ToString());

                RecepcionArticulos RecepcionArticulos = respuesta.Resultado as RecepcionArticulos;

                if (respuesta.IsSuccess)
                {
                    try
                    {
                        List<DetalleFactura> listaDetalleFactura = RecepcionArticulos.Articulo.DetalleFactura.ToList();

                        foreach (var item in listaDetalleFactura)
                        {
                            respuesta = await apiServicio.SeleccionarAsync<Response>(item.IdFactura.ToString(), new Uri(WebApp.BaseAddress), "/api/Factura");

                            respuesta.Resultado = JsonConvert.DeserializeObject<Factura>(respuesta.Resultado.ToString());

                            Factura factura = respuesta.Resultado as Factura;

                            var respuestaOtra = await apiServicio.SeleccionarAsync<Response>(factura.Numero.ToString(), new Uri(WebApp.BaseAddress), "/api/FacturasPorAltaProveeduria");

                            if ((respuestaOtra == null) && (respuesta.IsSuccess))
                            {
                                bool siEsta = false;

                                foreach (var _item in ListadoFacturas)
                                {
                                    if (_item.IdFactura == factura.IdFactura)
                                    {
                                        siEsta = true;
                                        break;
                                    }
                                }

                                if (siEsta)
                                {

                                } else
                                {
                                    ListadoFacturas.Add(factura);
                                }                                
                            }
                        }

                        ViewBag.data = ListadoFacturas;

                        return PartialView("_FacturasExcluidas"); //return PartialView("_FacturasExcluidas", ListadoFacturas);
                    }
                    catch (Exception ex)
                    {
                        await GuardarLogService.SaveLogEntry(new LogEntryTranfer
                        {
                            ApplicationName = Convert.ToString(Aplicacion.WebAppRM),
                            Message = "Listando facturas",
                            ExceptionTrace = ex,
                            LogCategoryParametre = Convert.ToString(LogCategoryParameter.Create),
                            LogLevelShortName = Convert.ToString(LogLevelParameter.ERR),
                            UserName = "Usuario APP WebAppTh"
                        });

                        return BadRequest();
                    }
                }

                return BadRequest();
            }
            catch (Exception ex)
            {
                await GuardarLogService.SaveLogEntry(new LogEntryTranfer
                {
                    ApplicationName = Convert.ToString(Aplicacion.WebAppRM),
                    Message = "Listando un objeto de RecepcionArticulos",
                    ExceptionTrace = ex,
                    LogCategoryParametre = Convert.ToString(LogCategoryParameter.Create),
                    LogLevelShortName = Convert.ToString(LogLevelParameter.ERR),
                    UserName = "Usuario APP WebAppTh"
                });

                return BadRequest();
            }
        }

        public async Task<IActionResult> CargarTablaFacturasIncluidas(int ID)
        {
            try
            {
                var respuesta = await apiServicio.SeleccionarAsync<Response>(ID.ToString(), new Uri(WebApp.BaseAddress), "/api/RecepcionArticulo");

                respuesta.Resultado = JsonConvert.DeserializeObject<RecepcionArticulos>(respuesta.Resultado.ToString());

                RecepcionArticulos RecepcionArticulos = respuesta.Resultado as RecepcionArticulos;

                if (respuesta.IsSuccess)
                {
                    try
                    {
                        List<DetalleFactura> listaDetalleFactura = RecepcionArticulos.Articulo.DetalleFactura.ToList();

                        foreach (var item in listaDetalleFactura)
                        {
                            respuesta = await apiServicio.SeleccionarAsync<Response>(item.IdFactura.ToString(), new Uri(WebApp.BaseAddress), "/api/Factura");

                            respuesta.Resultado = JsonConvert.DeserializeObject<Factura>(respuesta.Resultado.ToString());

                            Factura factura = respuesta.Resultado as Factura;

                            var respuestaOtra = await apiServicio.SeleccionarAsync<Response>(factura.Numero.ToString(), new Uri(WebApp.BaseAddress), "/api/FacturasPorAltaProveeduria");

                            if (respuestaOtra != null)
                            {
                                bool siEsta = false;

                                foreach (var _item in ListadoFacturasSeleccionadas)
                                {
                                    if (_item.IdFactura == factura.IdFactura)
                                    {
                                        siEsta = true;
                                        break;
                                    }
                                }

                                if (siEsta)
                                {

                                } else
                                {
                                    ListadoFacturasSeleccionadas.Add(factura);
                                }                                
                            }
                        }

                        ViewBag.data = ListadoFacturasSeleccionadas;

                        return PartialView("_FacturasIncluidas"); //return PartialView("_FacturasExcluidas", ListadoFacturas);
                    }
                    catch (Exception ex)
                    {
                        await GuardarLogService.SaveLogEntry(new LogEntryTranfer
                        {
                            ApplicationName = Convert.ToString(Aplicacion.WebAppRM),
                            Message = "Listando facturas",
                            ExceptionTrace = ex,
                            LogCategoryParametre = Convert.ToString(LogCategoryParameter.Create),
                            LogLevelShortName = Convert.ToString(LogLevelParameter.ERR),
                            UserName = "Usuario APP WebAppTh"
                        });

                        return BadRequest();
                    }
                }

                return BadRequest();
            }
            catch (Exception ex)
            {
                await GuardarLogService.SaveLogEntry(new LogEntryTranfer
                {
                    ApplicationName = Convert.ToString(Aplicacion.WebAppRM),
                    Message = "Listando un objeto de RecepcionArticulos",
                    ExceptionTrace = ex,
                    LogCategoryParametre = Convert.ToString(LogCategoryParameter.Create),
                    LogLevelShortName = Convert.ToString(LogLevelParameter.ERR),
                    UserName = "Usuario APP WebAppTh"
                });

                return BadRequest();
            }
        }

        public async Task<IActionResult> IncluirFacturasEnAlta(int idFactura)
        {
            try
            {
                var respuesta = await apiServicio.SeleccionarAsync<Response>(idFactura.ToString(), new Uri(WebApp.BaseAddress), "/api/Factura");

                if (respuesta.IsSuccess)
                {
                    respuesta.Resultado = JsonConvert.DeserializeObject<Factura>(respuesta.Resultado.ToString());

                    Factura factura = respuesta.Resultado as Factura;
                    
                    ListadoFacturasSeleccionadas.Add(factura);

                    ViewBag.data = ListadoFacturasSeleccionadas;

                    return PartialView("_FacturasIncluidas");
                }

                return BadRequest();
            }
            catch (Exception ex)
            {
                await GuardarLogService.SaveLogEntry(new LogEntryTranfer
                {
                    ApplicationName = Convert.ToString(Aplicacion.WebAppRM),
                    Message = "Incluyendo una factura a un objeto de Alta",
                    ExceptionTrace = ex,
                    LogCategoryParametre = Convert.ToString(LogCategoryParameter.Create),
                    LogLevelShortName = Convert.ToString(LogLevelParameter.ERR),
                    UserName = "Usuario APP WebAppTh"
                });

                return BadRequest();
            }
        }

        public async Task<IActionResult> RefrescarTablaExcluidos(int idFactura)
        {
            var respuesta = await apiServicio.SeleccionarAsync<Response>(idFactura.ToString(), new Uri(WebApp.BaseAddress), "/api/Factura");

            if (respuesta.IsSuccess)
            {
                respuesta.Resultado = JsonConvert.DeserializeObject<Factura>(respuesta.Resultado.ToString());

                Factura factura = respuesta.Resultado as Factura;

                List<Factura> temporal = new List<Factura>();

                foreach (var item in ListadoFacturas)
                {
                    if (item.IdFactura != factura.IdFactura)
                    {
                        temporal.Add(item);
                    }
                }

                ListadoFacturas = temporal;

                ViewBag.data = ListadoFacturas;

                return PartialView("_FacturasExcluidas");
            }

            return BadRequest();
        }

        public async Task<IActionResult> ExcluirFacturasEnAlta(int idFactura)
        {
            try
            {
                var respuesta = await apiServicio.SeleccionarAsync<Response>(idFactura.ToString(), new Uri(WebApp.BaseAddress), "/api/Factura");

                if (respuesta.IsSuccess)
                {
                    respuesta.Resultado = JsonConvert.DeserializeObject<Factura>(respuesta.Resultado.ToString());

                    Factura factura = respuesta.Resultado as Factura;

                    ListadoFacturas.Add(factura);

                    ViewBag.data = ListadoFacturas;

                    return PartialView("_FacturasExcluidas");
                }

                return BadRequest();
            }
            catch (Exception ex)
            {
                await GuardarLogService.SaveLogEntry(new LogEntryTranfer
                {
                    ApplicationName = Convert.ToString(Aplicacion.WebAppRM),
                    Message = "Excluyendo una factura a un objeto de Alta",
                    ExceptionTrace = ex,
                    LogCategoryParametre = Convert.ToString(LogCategoryParameter.Create),
                    LogLevelShortName = Convert.ToString(LogLevelParameter.ERR),
                    UserName = "Usuario APP WebAppTh"
                });

                return BadRequest();
            }
        }

        public async Task<IActionResult> RefrescarTablaIncluidos(int idFactura)
        {
            var respuesta = await apiServicio.SeleccionarAsync<Response>(idFactura.ToString(), new Uri(WebApp.BaseAddress), "/api/Factura");

            if (respuesta.IsSuccess)
            {
                respuesta.Resultado = JsonConvert.DeserializeObject<Factura>(respuesta.Resultado.ToString());

                Factura factura = respuesta.Resultado as Factura;

                List<Factura> temporal = new List<Factura>();

                foreach (var item in ListadoFacturasSeleccionadas)
                {
                    if (item.IdFactura != factura.IdFactura)
                    {
                        temporal.Add(item);
                    }
                }

                ListadoFacturasSeleccionadas = temporal;

                ViewBag.data = ListadoFacturasSeleccionadas;

                return PartialView("_FacturasIncluidas");
            }

            return BadRequest();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> AprobarAltaArticulo(RecepcionArticulos recepcionArticulos)
        {
            try
            { 
                int idRecepcionArticulo = recepcionArticulos.IdRecepcionArticulos;
                int idArticulo = recepcionArticulos.IdArticulo;
                int idProveedor = recepcionArticulos.IdProveedor;

                DateTime fechaAlta = DateTime.Now;

                AltaProveeduria alta = new AltaProveeduria { IdArticulo = idArticulo, IdProveedor = idProveedor, Acreditacion = null, FechaAlta = fechaAlta };

                Response response = new Response();

                response = await apiServicio.InsertarAsync(alta,
                                                                 new Uri(WebApp.BaseAddress),
                                                                 "/api/AltaProveeduria/InsertarAltaProveeduria");

                if (response.IsSuccess)
                {
                    List<AltaProveeduria> listaAltaProveeduria = await apiServicio.Listar<AltaProveeduria>(new Uri(WebApp.BaseAddress), "/api/AltaProveeduria/ListarAltasProveeduria");

                    var idAlta = listaAltaProveeduria.Where(c => c.IdArticulo == idArticulo && c.FechaAlta == fechaAlta).SingleOrDefault().IdAlta;

                    foreach (var item in ListadoFacturasSeleccionadas)
                    {
                        FacturasPorAltaProveeduria facturasPorAltaProveeduria = new FacturasPorAltaProveeduria { IdAlta = idAlta, NumeroFactura = item.Numero };

                        try
                        {
                            response = await apiServicio.InsertarAsync(facturasPorAltaProveeduria, new Uri(WebApp.BaseAddress), "/api/FacturaPorAltaProveeduria/InsertarFacturasPorAltaProveeduria");

                            if (response.IsSuccess)
                            {
                                
                            } else
                            {
                                try
                                {
                                    var eliminar = await apiServicio.EliminarAsync(idAlta.ToString(), new Uri(WebApp.BaseAddress), "/api/AltaProveeduria");

                                    if (eliminar.IsSuccess)
                                    {
                                        break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    await GuardarLogService.SaveLogEntry(new LogEntryTranfer
                                    {
                                        ApplicationName = Convert.ToString(Aplicacion.WebAppRM),
                                        Message = "Eliminando un objeto de AltaProveeduria",
                                        ExceptionTrace = ex,
                                        LogCategoryParametre = Convert.ToString(LogCategoryParameter.Create),
                                        LogLevelShortName = Convert.ToString(LogLevelParameter.ERR),
                                        UserName = "Usuario APP WebAppTh"
                                    });

                                    return BadRequest();
                                }                                
                            }
                        }
                        catch (Exception ex)
                        {
                            await GuardarLogService.SaveLogEntry(new LogEntryTranfer
                            {
                                ApplicationName = Convert.ToString(Aplicacion.WebAppRM),
                                Message = "Insertando un objeto de FacturasPorAltaProveeduria",
                                ExceptionTrace = ex,
                                LogCategoryParametre = Convert.ToString(LogCategoryParameter.Create),
                                LogLevelShortName = Convert.ToString(LogLevelParameter.ERR),
                                UserName = "Usuario APP WebAppTh"
                            });

                            return BadRequest();
                        }
                    }

                    return View("FormularioAltaArticulo");
                }
            }
            catch (Exception ex)
            {
                await GuardarLogService.SaveLogEntry(new LogEntryTranfer
                {
                    ApplicationName = Convert.ToString(Aplicacion.WebAppRM),
                    Message = "Listando un objeto de RecepcionArticulos",
                    ExceptionTrace = ex,
                    LogCategoryParametre = Convert.ToString(LogCategoryParameter.Create),
                    LogLevelShortName = Convert.ToString(LogLevelParameter.ERR),
                    UserName = "Usuario APP WebAppTh"
                });

                return BadRequest();
            }

            return View();
        }

        public async Task<IActionResult> IngresarFacturas()
        {
            var lista = new List<MaestroArticuloSucursal>();
            try
            {
                lista = await apiServicio.Listar<MaestroArticuloSucursal>(new Uri(WebApp.BaseAddress)
                                                                    , "/api/MaestroArticuloSucursal/ListarMaestroArticuloSucursal");
                return View("IngresarFacturas", lista);
            }
            catch (Exception ex)
            {
                await GuardarLogService.SaveLogEntry(new LogEntryTranfer
                {
                    ApplicationName = Convert.ToString(Aplicacion.WebAppRM),
                    Message = "Listando maestros de art�culos de sucursal",
                    ExceptionTrace = ex,
                    LogCategoryParametre = Convert.ToString(LogCategoryParameter.NetActivity),
                    LogLevelShortName = Convert.ToString(LogLevelParameter.ERR),
                    UserName = "Usuario APP webappth"
                });
                return BadRequest();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarFacturas(DetalleFactura detalleFactura)
        {
            int idMAS = detalleFactura.Factura.IdMaestroArticuloSucursal;
            int idProveedor = detalleFactura.Factura.IdProveedor;
            string numero = detalleFactura.Factura.Numero;

            int cantidad = detalleFactura.Cantidad;

            decimal ? precio = decimal.Parse(Request.Form["Precio"].ToString().Replace('.', ','));

            try
            {
                Factura factura = new Factura();
                factura.IdMaestroArticuloSucursal = idMAS;
                factura.IdProveedor = idProveedor;
                factura.Numero = numero;

                Response response = new Response();

                response = await apiServicio.InsertarAsync(factura, new Uri(WebApp.BaseAddress)
                                                                    , "/api/Factura/InsertarFactura");

                if (response.IsSuccess)
                {
                    try
                    {
                        var respuesta = await apiServicio.SeleccionarAsync<Response>(numero, new Uri(WebApp.BaseAddress),
                                                                                        "/api/Factura/FacturaPorNumero");

                        respuesta.Resultado = JsonConvert.DeserializeObject<Factura>(respuesta.Resultado.ToString());

                        Factura respuestaFactura = respuesta.Resultado as Factura;

                        try
                        {
                            //detalleFactura.Factura = respuestaFactura;
                            detalleFactura.IdFactura = respuestaFactura.IdFactura;
                            detalleFactura.Precio = precio;

                            response = await apiServicio.InsertarAsync(detalleFactura, new Uri(WebApp.BaseAddress)
                                                                    , "/api/DetalleFactura/InsertarDetalleFactura");

                            if (response.IsSuccess)
                            {
                                return RedirectToAction("IngresarFacturas");
                            }
                        }
                        catch (Exception ex)
                        {
                            await GuardarLogService.SaveLogEntry(new LogEntryTranfer
                            {
                                ApplicationName = Convert.ToString(Aplicacion.WebAppRM),
                                Message = "Insertando Detalle de una Factura",
                                ExceptionTrace = ex,
                                LogCategoryParametre = Convert.ToString(LogCategoryParameter.NetActivity),
                                LogLevelShortName = Convert.ToString(LogLevelParameter.ERR),
                                UserName = "Usuario APP webappth"
                            });
                            return BadRequest();
                        }
                        
                    }
                    catch (Exception ex)
                    {
                        await GuardarLogService.SaveLogEntry(new LogEntryTranfer
                        {
                            ApplicationName = Convert.ToString(Aplicacion.WebAppRM),
                            Message = "Obteniendo factura por Numero",
                            ExceptionTrace = ex,
                            LogCategoryParametre = Convert.ToString(LogCategoryParameter.NetActivity),
                            LogLevelShortName = Convert.ToString(LogLevelParameter.ERR),
                            UserName = "Usuario APP webappth"
                        });
                        return BadRequest();
                    }
                    
                }

                return RedirectToAction("FormularioAltaArticulo", idMAS);
            }
            catch (Exception ex)
            {
                await GuardarLogService.SaveLogEntry(new LogEntryTranfer
                {
                    ApplicationName = Convert.ToString(Aplicacion.WebAppRM),
                    Message = "Insertando una Factura",
                    ExceptionTrace = ex,
                    LogCategoryParametre = Convert.ToString(LogCategoryParameter.NetActivity),
                    LogLevelShortName = Convert.ToString(LogLevelParameter.ERR),
                    UserName = "Usuario APP webappth"
                });
                return BadRequest();
            }
        }

        public async Task<IActionResult> DetallesFactura(int ID)
        {
            var detalleFactura = new DetalleFactura();
            try
            {
                var listaProveedor = await apiServicio.Listar<Proveedor>(new Uri(WebApp.BaseAddress), "/api/Proveedor/ListarProveedores");
                var tlistaProveedor = listaProveedor.Select(c => new { IdProveedor = c.IdProveedor, NombreApellidos = String.Format("{0} {1}", c.Nombre, c.Apellidos) });
                ViewData["listaProveedor"] = new SelectList(tlistaProveedor, "IdProveedor", "NombreApellidos");
                
                try
                {
                    var listaArticulos = new SelectList(await apiServicio.Listar<Articulo>(new Uri(WebApp.BaseAddress)
                                                                        , "/api/Articulo/ListarArticulos"), "IdArticulo", "Nombre");
                    ViewBag.listaArticulos = listaArticulos;
                    ViewBag.ID = ID;

                    return View("DetallesFactura", detalleFactura);
                }
                catch (Exception ex)
                {
                    await GuardarLogService.SaveLogEntry(new LogEntryTranfer
                    {
                        ApplicationName = Convert.ToString(Aplicacion.WebAppRM),
                        Message = "Listando art�culos en el ingreso de una factura",
                        ExceptionTrace = ex,
                        LogCategoryParametre = Convert.ToString(LogCategoryParameter.NetActivity),
                        LogLevelShortName = Convert.ToString(LogLevelParameter.ERR),
                        UserName = "Usuario APP webappth"
                    });
                    return BadRequest();
                }               
            }
            catch (Exception ex)
            {
                await GuardarLogService.SaveLogEntry(new LogEntryTranfer
                {
                    ApplicationName = Convert.ToString(Aplicacion.WebAppRM),
                    Message = "Listando proveedores en el ingreso de una factura",
                    ExceptionTrace = ex,
                    LogCategoryParametre = Convert.ToString(LogCategoryParameter.NetActivity),
                    LogLevelShortName = Convert.ToString(LogLevelParameter.ERR),
                    UserName = "Usuario APP webappth"
                });
                return BadRequest();
            }
        }
        #endregion

        #region Baja de Proveedur�a

        public async Task<IActionResult> ArticulosADarBaja()
        {
            var lista = new List<RecepcionArticulos>();
            try
            {
                lista = await apiServicio.Listar<RecepcionArticulos>(new Uri(WebApp.BaseAddress)
                                                                    , "/api/RecepcionArticulo/ListarRecepcionArticulos");

                var listaArticulosRecepcionados = lista.Select(c => c).ToList();

                return View("ArticulosBaja", listaArticulosRecepcionados);
            }
            catch (Exception ex)
            {
                await GuardarLogService.SaveLogEntry(new LogEntryTranfer
                {
                    ApplicationName = Convert.ToString(Aplicacion.WebAppRM),
                    Message = "Listando articulos recepcionados",
                    ExceptionTrace = ex,
                    LogCategoryParametre = Convert.ToString(LogCategoryParameter.NetActivity),
                    LogLevelShortName = Convert.ToString(LogLevelParameter.ERR),
                    UserName = "Usuario APP webappth"
                });
                return BadRequest();
            }
        }

        public async Task<IActionResult> FormularioBajaArticulo(int ID)
        {
            try
            {
                var respuesta = await apiServicio.SeleccionarAsync<Response>(ID.ToString(), new Uri(WebApp.BaseAddress), "/api/RecepcionArticulo");

                respuesta.Resultado = JsonConvert.DeserializeObject<RecepcionArticulos>(respuesta.Resultado.ToString());

                RecepcionArticulos RecepcionArticulos = respuesta.Resultado as RecepcionArticulos;

                //ViewBag.Acreditacion = new SelectList(new List<string> { "Facturas", "Documentos" });

                if (respuesta.IsSuccess)
                {
                    return View("FormularioBajaArticulo", RecepcionArticulos);
                }
            }
            catch (Exception ex)
            {
                await GuardarLogService.SaveLogEntry(new LogEntryTranfer
                {
                    ApplicationName = Convert.ToString(Aplicacion.WebAppRM),
                    Message = "Listando un objeto de RecepcionArticulos",
                    ExceptionTrace = ex,
                    LogCategoryParametre = Convert.ToString(LogCategoryParameter.Create),
                    LogLevelShortName = Convert.ToString(LogLevelParameter.ERR),
                    UserName = "Usuario APP WebAppTh"
                });

                return BadRequest();
            }

            return View();

        }

        public async Task<IActionResult> AprobarBajaArticulo(int idRecepcionArticulo, int idProveedor, string archivo)
        {
            try
            {
                var respuesta = await apiServicio.SeleccionarAsync<Response>(idRecepcionArticulo.ToString(), new Uri(WebApp.BaseAddress), "/api/RecepcionArticulo");

                if (respuesta.IsSuccess)
                {
                    return View("FormularioBajaArticulo");
                }
            }
            catch (Exception ex)
            {
                await GuardarLogService.SaveLogEntry(new LogEntryTranfer
                {
                    ApplicationName = Convert.ToString(Aplicacion.WebAppRM),
                    Message = "Listando un objeto de RecepcionArticulos",
                    ExceptionTrace = ex,
                    LogCategoryParametre = Convert.ToString(LogCategoryParameter.Create),
                    LogLevelShortName = Convert.ToString(LogLevelParameter.ERR),
                    UserName = "Usuario APP WebAppTh"
                });

                return BadRequest();
            }

            return View();
        }
        #endregion


        #region AJAX_ClaseArticulo
        public async Task<SelectList> ObtenerSelectListClaseArticulo(int idTipoArticulo)
        {
            try
            {
                var listaClaseArticulo = await apiServicio.Listar<ClaseArticulo>(new Uri(WebApp.BaseAddress), "/api/ClaseArticulo/ListarClaseArticulo");
                listaClaseArticulo = idTipoArticulo != -1 ? listaClaseArticulo.Where(c => c.IdTipoArticulo == idTipoArticulo).ToList() : new List<ClaseArticulo>();
                return new SelectList(listaClaseArticulo, "IdClaseArticulo", "Nombre");
            }
            catch (Exception)
            {
                return new SelectList(new List<ClaseArticulo>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> ClaseArticulo_SelectResult(int idTipoArticulo)
        {
            ViewBag.ClaseArticulo = await ObtenerSelectListClaseArticulo(idTipoArticulo);
            return PartialView("_ClaseArticuloSelect", new RecepcionArticulos());
        }
        #endregion

        #region AJAX_SubClaseArticulo
        public async Task<SelectList> ObtenerSelectListSubClaseArticulo(int idClaseArticulo)
        {
            try
            {
                var listaSubClaseArticulo = await apiServicio.Listar<SubClaseArticulo>(new Uri(WebApp.BaseAddress), "/api/SubClaseArticulo/ListarSubClaseArticulos");
                listaSubClaseArticulo = idClaseArticulo != -1 ? listaSubClaseArticulo.Where(c => c.IdClaseArticulo == idClaseArticulo).ToList() : new List<SubClaseArticulo>();
                return new SelectList(listaSubClaseArticulo, "IdSubClaseArticulo", "Nombre");
            }
            catch (Exception)
            {
                return new SelectList(new List<SubClaseArticulo>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> SubClaseArticulo_SelectResult(int idClaseArticulo)
        {
            ViewBag.SubClaseArticulo = await ObtenerSelectListSubClaseArticulo(idClaseArticulo);
            return PartialView("_SubClaseArticuloSelect", new RecepcionArticulos());
        }
        #endregion

        #region AJAX_Articulo
        public async Task<SelectList> ObtenerSelectListArticulo(int idSubClaseArticulo)
        {
            try
            {
                var listaArticulo = await apiServicio.Listar<Articulo>(new Uri(WebApp.BaseAddress), "/api/Articulo/ListarArticulos");
                listaArticulo = idSubClaseArticulo != -1 ? listaArticulo.Where(c => c.IdSubClaseArticulo == idSubClaseArticulo).ToList() : new List<Articulo>();
                return new SelectList(listaArticulo, "IdArticulo", "Nombre");
            }
            catch (Exception)
            {
                return new SelectList(new List<Articulo>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> Articulo_SelectResult(int idSubClaseArticulo)
        {
            ViewBag.Articulo = await ObtenerSelectListArticulo(idSubClaseArticulo);
            return PartialView("_ArticuloSelect", new RecepcionArticulos());
        }
        #endregion

        #region AJAX_Provincia
        public async Task<SelectList> ObtenerSelectListProvincia(int idPais)
        {
            try
            {
                var listaProvincia = await apiServicio.Listar<Provincia>(new Uri(WebApp.BaseAddress), "/api/Provincia/ListarProvincias");
                listaProvincia = idPais != -1 ? listaProvincia.Where(c => c.IdPais == idPais).ToList() : new List<Provincia>();
                return new SelectList(listaProvincia, "IdProvincia", "Nombre");
            }
            catch (Exception)
            {
                return new SelectList(new List<Provincia>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> Provincia_SelectResult(int idPais)
        {
            ViewBag.Provincia = await ObtenerSelectListProvincia(idPais);
            return PartialView("_ProvinciaSelect", new RecepcionArticulos());
        }
        #endregion

        #region AJAX_Ciudad
        public async Task<SelectList> ObtenerSelectListCiudad(int idProvincia)
        {
            try
            {
                var listaCiudad = await apiServicio.Listar<Ciudad>(new Uri(WebApp.BaseAddress), "/api/Ciudad/ListarCiudades");
                listaCiudad = idProvincia != -1 ? listaCiudad.Where(c => c.IdProvincia == idProvincia).ToList() : new List<Ciudad>();
                return new SelectList(listaCiudad, "IdCiudad", "Nombre");
            }
            catch (Exception)
            {
                return new SelectList(new List<Ciudad>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> Ciudad_SelectResult(int idProvincia)
        {
            ViewBag.Ciudad = await ObtenerSelectListCiudad(idProvincia);
            return PartialView("_CiudadSelect", new RecepcionArticulos());
        }
        #endregion

        #region AJAX_Sucursal
        public async Task<SelectList> ObtenerSelectListSucursal(int idCiudad)
        {
            try
            {
                var listaSucursal = await apiServicio.Listar<Sucursal>(new Uri(WebApp.BaseAddress), "/api/Sucursal/ListarSucursales");
                listaSucursal = idCiudad != -1 ? listaSucursal.Where(c => c.IdCiudad == idCiudad).ToList() : new List<Sucursal>();
                return new SelectList(listaSucursal, "IdSucursal", "Nombre");
            }
            catch (Exception)
            {
                return new SelectList(new List<Sucursal>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> Sucursal_SelectResult(int idCiudad)
        {
            ViewBag.Sucursal = await ObtenerSelectListSucursal(idCiudad);
            return PartialView("_SucursalSelect", new RecepcionArticulos());
        }
        #endregion

        #region AJAX_MaestroArticuloSucursal
        public async Task<SelectList> ObtenerSelectListMaestroArticuloSucursal(int idSucursal)
        {
            try
            {
                var listaMaestroArticuloSucursal = await apiServicio.Listar<MaestroArticuloSucursal>(new Uri(WebApp.BaseAddress), "/api/MaestroArticuloSucursal/ListarMaestroArticuloSucursal");
                listaMaestroArticuloSucursal = idSucursal != -1 ? listaMaestroArticuloSucursal.Where(c => c.IdSucursal == idSucursal).ToList() : new List<MaestroArticuloSucursal>();
                var tlistaMaestroArticuloSucursal = listaMaestroArticuloSucursal.Select(c => new { IdMaestroArticuloSucursal = c.IdMaestroArticuloSucursal, Maestro = String.Format("M�nimo: {0} - M�ximo: {1}", c.Minimo, c.Maximo) });
                return new SelectList(tlistaMaestroArticuloSucursal, "IdMaestroArticuloSucursal", "Maestro");
            }
            catch (Exception)
            {
                return new SelectList(new List<MaestroArticuloSucursal>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> MaestroArticuloSucursal_SelectResult(int idSucursal)
        {
            ViewBag.MaestroArticuloSucursal = await ObtenerSelectListMaestroArticuloSucursal(idSucursal);
            return PartialView("_MaestroArticuloSucursalSelect", new RecepcionArticulos());
        }
        #endregion
    }
}