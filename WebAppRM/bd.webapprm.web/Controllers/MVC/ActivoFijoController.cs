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
using Microsoft.AspNetCore.Http;

namespace bd.webapprm.web.Controllers.MVC
{
    public class ActivoFijoController : Controller
    {
        private readonly IApiServicio apiServicio;

        public ActivoFijoController(IApiServicio apiServicio)
        {
            this.apiServicio = apiServicio;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Recepcion");
        }

        public async Task<IActionResult> ActivoValidacionTecnica()
        {
            var lista = new List<RecepcionActivoFijoDetalle>();
            try
            {
                lista = await apiServicio.Listar<RecepcionActivoFijoDetalle>(new Uri(WebApp.BaseAddress)
                                                                    , "/api/RecepcionActivoFijo/ListarRecepcionActivoFijo");

                var listaActivosFijosValidacionTecnica = lista.Where(c => c.Estado.Nombre == "Validaci�n T�cnica").ToList();
                return View(listaActivosFijosValidacionTecnica);
            }
            catch (Exception ex)
            {
                await GuardarLogService.SaveLogEntry(new LogEntryTranfer
                {
                    ApplicationName = Convert.ToString(Aplicacion.WebAppRM),
                    Message = "Listando activos fijos que requieren validaci�n t�cnica",
                    ExceptionTrace = ex,
                    LogCategoryParametre = Convert.ToString(LogCategoryParameter.NetActivity),
                    LogLevelShortName = Convert.ToString(LogLevelParameter.ERR),
                    UserName = "Usuario APP webappth"
                });
                return BadRequest();
            }
        }

        public async Task<IActionResult> Recepcion()
        {
            ViewData["TipoActivoFijo"] = new SelectList(await apiServicio.Listar<TipoActivoFijo>(new Uri(WebApp.BaseAddress), "/api/TipoActivoFijo/ListarTipoActivoFijos"), "IdTipoActivoFijo", "Nombre");
            ViewData["ClaseActivoFijo"] = await ObtenerSelectListClaseActivoFijo((ViewData["TipoActivoFijo"] as SelectList).FirstOrDefault() != null ? int.Parse((ViewData["TipoActivoFijo"] as SelectList).FirstOrDefault().Value) : -1);
            ViewData["SubClaseActivoFijo"] = await ObtenerSelectListSubClaseActivoFijo((ViewData["ClaseActivoFijo"] as SelectList).FirstOrDefault() != null ? int.Parse((ViewData["ClaseActivoFijo"] as SelectList).FirstOrDefault().Value) : -1);
            ViewData["MotivoRecepcion"] = new SelectList(await apiServicio.Listar<MotivoRecepcion>(new Uri(WebApp.BaseAddress), "/api/MotivoRecepcion/ListarMotivoRecepcion"), "IdMotivoRecepcion", "Descripcion");
            
            ViewData["Pais"] = new SelectList(await apiServicio.Listar<Pais>(new Uri(WebApp.BaseAddress), "/api/Pais/ListarPaises"), "IdPais", "Nombre");
            ViewData["Provincia"] = await ObtenerSelectListProvincia((ViewData["Pais"] as SelectList).FirstOrDefault() != null ? int.Parse((ViewData["Pais"] as SelectList).FirstOrDefault().Value) : -1);
            ViewData["Ciudad"] = await ObtenerSelectListCiudad((ViewData["Provincia"] as SelectList).FirstOrDefault() != null ? int.Parse((ViewData["Provincia"] as SelectList).FirstOrDefault().Value) : -1);
            ViewData["Sucursal"] = await ObtenerSelectListSucursal((ViewData["Ciudad"] as SelectList).FirstOrDefault() != null ? int.Parse((ViewData["Ciudad"] as SelectList).FirstOrDefault().Value) : -1);

            var listaProveedor = await apiServicio.Listar<Proveedor>(new Uri(WebApp.BaseAddress), "/api/Proveedor/ListarProveedores");
            var tlistaProveedor = listaProveedor.Select(c => new { IdProveedor = c.IdProveedor, NombreApellidos = String.Format("{0} {1}", c.Nombre, c.Apellidos) });
            ViewData["Proveedor"] = new SelectList(tlistaProveedor, "IdProveedor", "NombreApellidos");

            var listaEmpleado = await apiServicio.Listar<Empleado>(new Uri(WebApp.BaseAddress), "/api/Empleado/ListarEmpleados");
            var tlistaEmpleado = listaEmpleado.Select(c => new { IdEmpleado = c.IdEmpleado, NombreApellidos = String.Format("{0} {1}", c.Persona.Nombres, c.Persona.Apellidos) });
            ViewData["Empleado"] = new SelectList(tlistaEmpleado, "IdEmpleado", "NombreApellidos");

            ViewData["Marca"] = new SelectList(await apiServicio.Listar<Marca>(new Uri(WebApp.BaseAddress), "/api/Marca/ListarMarca"), "IdMarca", "Nombre");
            ViewData["Modelo"] = await ObtenerSelectListModelo((ViewData["Marca"] as SelectList).FirstOrDefault() != null ? int.Parse((ViewData["Marca"] as SelectList).FirstOrDefault().Value) : -1);
            ViewData["UnidadMedida"] = new SelectList(await apiServicio.Listar<UnidadMedida>(new Uri(WebApp.BaseAddress), "/api/UnidadMedida/ListarUnidadMedida"), "IdUnidadMedida", "Nombre");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Recepcion(RecepcionActivoFijoDetalle recepcionActivoFijoDetalle)
        {
            Response response = new Response();
            try
            {
                var listaTipoActivoFijo = await apiServicio.Listar<TipoActivoFijo>(new Uri(WebApp.BaseAddress), "/api/TipoActivoFijo/ListarTipoActivoFijos");
                var listaMarca = await apiServicio.Listar<Marca>(new Uri(WebApp.BaseAddress), "/api/Marca/ListarMarca");

                var listaSubClaseActivoFijo = await apiServicio.Listar<SubClaseActivoFijo>(new Uri(WebApp.BaseAddress), "/api/SubClaseActivoFijo/ListarSubClasesActivoFijo");
                recepcionActivoFijoDetalle.RecepcionActivoFijo.SubClaseActivoFijo = listaSubClaseActivoFijo.SingleOrDefault(c => c.IdSubClaseActivoFijo == recepcionActivoFijoDetalle?.RecepcionActivoFijo?.IdSubClaseActivoFijo);

                if (recepcionActivoFijoDetalle.RecepcionActivoFijo.SubClaseActivoFijo == null)
                {
                    try
                    {
                        recepcionActivoFijoDetalle.RecepcionActivoFijo.SubClaseActivoFijo = new SubClaseActivoFijo();
                        int IdClaseActivoFijo = int.Parse(Request.Form["RecepcionActivoFijo.SubClaseActivoFijo.IdClaseActivoFijo"].ToString());

                        var listaClaseActivoFijo = await apiServicio.Listar<ClaseActivoFijo>(new Uri(WebApp.BaseAddress), "/api/ClaseActivoFijo/ListarClaseActivoFijo");
                        recepcionActivoFijoDetalle.RecepcionActivoFijo.SubClaseActivoFijo.ClaseActivoFijo = listaClaseActivoFijo.SingleOrDefault(c => c.IdClaseActivoFijo == IdClaseActivoFijo);
                        recepcionActivoFijoDetalle.RecepcionActivoFijo.SubClaseActivoFijo.IdClaseActivoFijo = IdClaseActivoFijo;
                    }
                    catch (Exception)
                    {
                        recepcionActivoFijoDetalle.RecepcionActivoFijo.SubClaseActivoFijo.ClaseActivoFijo = new ClaseActivoFijo();
                        try
                        {
                            int IdTipoActivoFijo = int.Parse(Request.Form["RecepcionActivoFijo.SubClaseActivoFijo.ClaseActivoFijo.IdTipoActivoFijo"].ToString());
                            recepcionActivoFijoDetalle.RecepcionActivoFijo.SubClaseActivoFijo.ClaseActivoFijo.TipoActivoFijo = listaTipoActivoFijo.SingleOrDefault(c => c.IdTipoActivoFijo == IdTipoActivoFijo);
                            recepcionActivoFijoDetalle.RecepcionActivoFijo.SubClaseActivoFijo.ClaseActivoFijo.IdTipoActivoFijo = IdTipoActivoFijo;
                        }
                        catch (Exception)
                        { }
                    }
                }

                var listaModelo = await apiServicio.Listar<Modelo>(new Uri(WebApp.BaseAddress), "/api/Modelo/ListarModelos");
                recepcionActivoFijoDetalle.ActivoFijo.Modelo = listaModelo.SingleOrDefault(c => c.IdModelo == recepcionActivoFijoDetalle?.ActivoFijo?.IdModelo);

                if (recepcionActivoFijoDetalle.ActivoFijo.Modelo == null)
                {
                    try
                    {
                        recepcionActivoFijoDetalle.ActivoFijo.Modelo = new Modelo();
                        int IdMarca = int.Parse(Request.Form["ActivoFijo.Modelo.IdMarca"].ToString());
                        recepcionActivoFijoDetalle.ActivoFijo.Modelo.Marca = listaMarca.SingleOrDefault(c => c.IdMarca == IdMarca);
                        recepcionActivoFijoDetalle.ActivoFijo.Modelo.IdMarca = IdMarca;
                    }
                    catch (Exception)
                    { }
                }

                var listaUnidadMedida = await apiServicio.Listar<UnidadMedida>(new Uri(WebApp.BaseAddress), "/api/UnidadMedida/ListarUnidadMedida");
                recepcionActivoFijoDetalle.ActivoFijo.UnidadMedida = listaUnidadMedida.SingleOrDefault(c => c.IdUnidadMedida == recepcionActivoFijoDetalle.ActivoFijo.IdUnidadMedida);

                TryValidateModel(recepcionActivoFijoDetalle);

                /*response = await apiServicio.InsertarAsync(activoFijo,
                                                             new Uri(WebApp.BaseAddress),
                                                             "/api/ActivoFijo/InsertarActivoFijo");
                if (response.IsSuccess)
                {

                    var responseLog = await GuardarLogService.SaveLogEntry(new LogEntryTranfer
                    {
                        ApplicationName = Convert.ToString(Aplicacion.WebAppRM),
                        ExceptionTrace = null,
                        Message = "Se ha creado un activo fijo",
                        UserName = "Usuario 1",
                        LogCategoryParametre = Convert.ToString(LogCategoryParameter.Create),
                        LogLevelShortName = Convert.ToString(LogLevelParameter.ADV),
                        EntityID = string.Format("{0} {1}", "Activo Fijo:", activoFijo.IdActivoFijo),
                    });

                    return RedirectToAction("Index");
                }*/

                ViewData["Error"] = response.Message;
                ViewData["TipoActivoFijo"] = new SelectList(listaTipoActivoFijo, "IdTipoActivoFijo", "Nombre");
                ViewData["ClaseActivoFijo"] = await ObtenerSelectListClaseActivoFijo(recepcionActivoFijoDetalle?.RecepcionActivoFijo?.SubClaseActivoFijo?.ClaseActivoFijo?.TipoActivoFijo?.IdTipoActivoFijo ?? -1);
                ViewData["SubClaseActivoFijo"] = await ObtenerSelectListSubClaseActivoFijo(recepcionActivoFijoDetalle?.RecepcionActivoFijo?.SubClaseActivoFijo?.ClaseActivoFijo?.IdClaseActivoFijo ?? -1);
                ViewData["MotivoRecepcion"] = new SelectList(await apiServicio.Listar<MotivoRecepcion>(new Uri(WebApp.BaseAddress), "/api/MotivoRecepcion/ListarMotivoRecepcion"), "IdMotivoRecepcion", "Descripcion");

                var listaProveedor = await apiServicio.Listar<Proveedor>(new Uri(WebApp.BaseAddress), "/api/Proveedor/ListarProveedores");
                var tlistaProveedor = listaProveedor.Select(c => new { IdProveedor = c.IdProveedor, NombreApellidos = String.Format("{0} {1}", c.Nombre, c.Apellidos) });
                ViewData["Proveedor"] = new SelectList(tlistaProveedor, "IdProveedor", "NombreApellidos");

                var listaEmpleado = await apiServicio.Listar<Empleado>(new Uri(WebApp.BaseAddress), "/api/Empleado/ListarEmpleados");
                var tlistaEmpleado = listaEmpleado.Select(c => new { IdEmpleado = c.IdEmpleado, NombreApellidos = String.Format("{0} {1}", c.Persona.Nombres, c.Persona.Apellidos) });
                ViewData["Empleado"] = new SelectList(tlistaEmpleado, "IdEmpleado", "NombreApellidos");

                ViewData["Marca"] = new SelectList(listaMarca, "IdMarca", "Nombre");
                ViewData["Modelo"] = await ObtenerSelectListModelo(recepcionActivoFijoDetalle?.ActivoFijo?.Modelo?.Marca?.IdMarca ?? -1);
                ViewData["UnidadMedida"] = new SelectList(await apiServicio.Listar<UnidadMedida>(new Uri(WebApp.BaseAddress), "/api/UnidadMedida/ListarUnidadMedida"), "IdUnidadMedida", "Nombre");

                return View(recepcionActivoFijoDetalle);
            }
            catch (Exception ex)
            {
                await GuardarLogService.SaveLogEntry(new LogEntryTranfer
                {
                    ApplicationName = Convert.ToString(Aplicacion.WebAppRM),
                    Message = "Creando Activo Fijo",
                    ExceptionTrace = ex,
                    LogCategoryParametre = Convert.ToString(LogCategoryParameter.Create),
                    LogLevelShortName = Convert.ToString(LogLevelParameter.ERR),
                    UserName = "Usuario APP WebAppTh"
                });

                return BadRequest();
            }
        }

        public async Task<IActionResult> ActivosFijosRecepcionados()
        {
            var lista = new List<RecepcionActivoFijoDetalle>();
            try
            {
                lista = await apiServicio.Listar<RecepcionActivoFijoDetalle>(new Uri(WebApp.BaseAddress)
                                                                    , "/api/RecepcionActivoFijo/ListarRecepcionActivoFijo");

                var listaActivosFijosRecepcionados = lista.Where(c => c.Estado.Nombre == "Recepcionado").ToList();
                return View(listaActivosFijosRecepcionados);
            }
            catch (Exception ex)
            {
                await GuardarLogService.SaveLogEntry(new LogEntryTranfer
                {
                    ApplicationName = Convert.ToString(Aplicacion.WebAppRM),
                    Message = "Listando activos fijos con estado Recepcionado",
                    ExceptionTrace = ex,
                    LogCategoryParametre = Convert.ToString(LogCategoryParameter.NetActivity),
                    LogLevelShortName = Convert.ToString(LogLevelParameter.ERR),
                    UserName = "Usuario APP webappth"
                });
                return BadRequest();
            }
        }

        public async Task<IActionResult> RecepcionadosSinPoliza()
        {
            var lista = new List<RecepcionActivoFijoDetalle>();
            try
            {
                lista = await apiServicio.Listar<RecepcionActivoFijoDetalle>(new Uri(WebApp.BaseAddress)
                                                                    , "/api/RecepcionActivoFijo/ListarRecepcionActivoFijo");

                var listaActivosFijosRecepcionados = lista.Where(c => c.Estado.Nombre == "Recepcionado" && c.NumeroPoliza == "N/A").ToList();
                return View(listaActivosFijosRecepcionados);
            }
            catch (Exception ex)
            {
                await GuardarLogService.SaveLogEntry(new LogEntryTranfer
                {
                    ApplicationName = Convert.ToString(Aplicacion.WebAppRM),
                    Message = "Listando activos fijos con estado Recepcionado sin n�mero de p�liza asignado",
                    ExceptionTrace = ex,
                    LogCategoryParametre = Convert.ToString(LogCategoryParameter.NetActivity),
                    LogLevelShortName = Convert.ToString(LogLevelParameter.ERR),
                    UserName = "Usuario APP webappth"
                });
                return BadRequest();
            }
        }

        public async Task<IActionResult> AsignarPoliza(string id)
        {
            try
            {
                if (!string.IsNullOrEmpty(id))
                {
                    var respuesta = await apiServicio.SeleccionarAsync<Response>(id, new Uri(WebApp.BaseAddress),
                                                                  "/api/RecepcionActivoFijo/ListarRecepcionActivoFijo");


                    respuesta.Resultado = JsonConvert.DeserializeObject<RecepcionActivoFijoDetalle>(respuesta.Resultado.ToString());
                    if (respuesta.IsSuccess)
                    {
                        return View(respuesta.Resultado);
                    }

                }

                return BadRequest();
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AsignarPoliza(string id, RecepcionActivoFijoDetalle recepcionActivoFijoDetalle)
        {
            Response response = new Response();
            try
            {
                if (!string.IsNullOrEmpty(id))
                {
                    response = await apiServicio.EditarAsync(id, recepcionActivoFijoDetalle, new Uri(WebApp.BaseAddress),
                                                                 "/api/RecepcionActivoFijo/InsertarRecepcionActivoFijo");

                    if (response.IsSuccess)
                    {
                        await GuardarLogService.SaveLogEntry(new LogEntryTranfer
                        {
                            ApplicationName = Convert.ToString(Aplicacion.WebAppRM),
                            EntityID = string.Format("{0} : {1}", "Recepcion Activo Fijo Detalle", id),
                            LogCategoryParametre = Convert.ToString(LogCategoryParameter.Edit),
                            LogLevelShortName = Convert.ToString(LogLevelParameter.ADV),
                            Message = "Se ha actualizado un registro Activo Fijo",
                            UserName = "Usuario 1"
                        });

                        return RedirectToAction("Index");
                    }

                }
                return View(recepcionActivoFijoDetalle);
            }
            catch (Exception ex)
            {
                await GuardarLogService.SaveLogEntry(new LogEntryTranfer
                {
                    ApplicationName = Convert.ToString(Aplicacion.WebAppRM),
                    Message = "Asignando P�liza de Seguro",
                    ExceptionTrace = ex,
                    LogCategoryParametre = Convert.ToString(LogCategoryParameter.Edit),
                    LogLevelShortName = Convert.ToString(LogLevelParameter.ERR),
                    UserName = "Usuario APP webappth"
                });

                return BadRequest();
            }
        }

        #region AJAX_ClaseActivoFijo
        public async Task<SelectList> ObtenerSelectListClaseActivoFijo(int idTipoActivoFijo)
        {
            try
            {
                var listaClaseActivoFijo = await apiServicio.Listar<ClaseActivoFijo>(new Uri(WebApp.BaseAddress), "/api/ClaseActivoFijo/ListarClaseActivoFijo");
                listaClaseActivoFijo = idTipoActivoFijo != -1 ? listaClaseActivoFijo.Where(c => c.IdTipoActivoFijo == idTipoActivoFijo).ToList() : new List<ClaseActivoFijo>();
                return new SelectList(listaClaseActivoFijo, "IdClaseActivoFijo", "Nombre");
            }
            catch (Exception)
            {
                return new SelectList(new List<ClaseActivoFijo>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> ClaseActivoFijo_SelectResult(int idTipoActivoFijo)
        {
            ViewBag.ClaseActivoFijo = await ObtenerSelectListClaseActivoFijo(idTipoActivoFijo);
            return PartialView("_ClaseActivoFijoSelect", new RecepcionActivoFijoDetalle());
        }
        #endregion

        #region AJAX_SubClaseActivoFijo
        public async Task<SelectList> ObtenerSelectListSubClaseActivoFijo(int idClaseActivoFijo)
        {
            try
            {
                var listaSubClaseActivoFijo = await apiServicio.Listar<SubClaseActivoFijo>(new Uri(WebApp.BaseAddress), "/api/SubClaseActivoFijo/ListarSubClasesActivoFijo");
                listaSubClaseActivoFijo = idClaseActivoFijo != -1 ? listaSubClaseActivoFijo.Where(c => c.IdClaseActivoFijo == idClaseActivoFijo).ToList() : new List<SubClaseActivoFijo>();
                return new SelectList(listaSubClaseActivoFijo, "IdSubClaseActivoFijo", "Nombre");
            }
            catch (Exception)
            {
                return new SelectList(new List<SubClaseActivoFijo>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> SubClaseActivoFijo_SelectResult(int idClaseActivoFijo)
        {
            ViewBag.SubClaseActivoFijo = await ObtenerSelectListSubClaseActivoFijo(idClaseActivoFijo);
            return PartialView("_SubClaseActivoFijoSelect", new RecepcionActivoFijoDetalle());
        }
        #endregion

        #region AJAX_Modelo
        public async Task<SelectList> ObtenerSelectListModelo(int idMarca)
        {
            try
            {
                var listaModelo = await apiServicio.Listar<Modelo>(new Uri(WebApp.BaseAddress), "/api/Modelo/ListarModelos");
                listaModelo = idMarca != -1 ? listaModelo.Where(c => c.IdMarca == idMarca).ToList() : new List<Modelo>();
                return new SelectList(listaModelo, "IdModelo", "Nombre");
            }
            catch (Exception)
            {
                return new SelectList(new List<Modelo>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> Modelo_SelectResult(int idMarca)
        {
            ViewBag.Modelo = await ObtenerSelectListModelo(idMarca);
            return PartialView("_ModeloSelect", new RecepcionActivoFijoDetalle());
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
            return PartialView("_ProvinciaSelect", new RecepcionActivoFijoDetalle());
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
            return PartialView("_CiudadSelect", new RecepcionActivoFijoDetalle());
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
            return PartialView("_SucursalSelect", new RecepcionActivoFijoDetalle());
        }
        #endregion
    }
}