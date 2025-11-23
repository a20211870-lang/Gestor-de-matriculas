using FrontTA.Alumnos;
using FrontTA.SisProgWS;
using System;
using System.ComponentModel;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace FrontTA.Matricula
{
    public partial class EditarMatricula : Page
    {
        private static int idMatricula;
        private static int anio2;
        private MatriculaWSClient boMatricula = new MatriculaWSClient();
        private AlumnoWSClient boAlumno = new AlumnoWSClient();
        private PeriodoXAulaWSClient boPeriodoAula = new PeriodoXAulaWSClient();
        protected void Page_Load(object sender, EventArgs e)
        {

            if (!IsPostBack)
            {
                // Año de la matrícula (desde QS o vigente)
                int anio = DateTime.Now.Year;
                int.TryParse(Request.QueryString["anio"], out anio);
                lblAnio.Text = anio.ToString();
                ViewState["anio"] = anio;
                anio2 = anio;
                idMatricula = 0;
                int.TryParse(Request.QueryString["id"], out idMatricula);
                ViewState["idMatricula"] = idMatricula;

                // Cargar "matrícula" simulada (por id en QS idealmente)
                CargarDatosDeEjemplo();

                // Poblamos las tablas de los modals (GridView, 3+ filas)
                var list = boAlumno.listarAlumnosTodos();
                BindingList<alumno> alumnos = new BindingList<alumno>(list);
                gvAlumnoModal.DataSource = alumnos;
                gvAlumnoModal.DataBind();

                var list2 = boMatricula.listarPeriodoXAulasParaAsignarMatriculas();
                BindingList<periodoXAula> aulas = new BindingList<periodoXAula>(list2);
                gvAulaModal.DataSource = aulas;
                gvAulaModal.DataBind();
            }

        }

        private void CargarDatosDeEjemplo()
        {
            // Normalmente: buscarías por IdMatricula en la BD + año

            matricula mat = boMatricula.obtenerMatriculaPorId(idMatricula);
            txtAlumno.Text = mat.alumno.padres.apellido_paterno + " " + mat.alumno.padres.apellido_materno + ", " +
                mat.alumno.nombre;
            hdnAlumnoId.Value = mat.alumno.alumno_id.ToString();
            txtMonto.Text = "S/. " + mat.alumno.pension_base;
            ddlActivo.SelectedValue = mat.activo.ToString();
            txtAula.Text = mat.periodo_Aula.aula.nombre;
            int periodo_grado = mat.periodo_Aula.periodo.periodo_academico_id;
            hdnAulaId.Value = "0";
            periodoXAula periodo = boPeriodoAula.obtenerPeriodoXAulaPorId(mat.periodo_Aula.periodo.periodo_academico_id);
            int capacidad = periodo.vacantes_ocupadas + periodo.vacantes_disponibles;
            txtCapacidad.Text = capacidad.ToString();
            txtVacantes.Text = periodo.vacantes_disponibles.ToString();
        }




        protected void gvAlumnoModal_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                e.Row.CssClass += " data-row";

                if (e.Row.RowIndex < gvAlumnoModal.DataKeys.Count)
                {
                    var id = gvAlumnoModal.DataKeys[e.Row.RowIndex].Value.ToString();
                    e.Row.Attributes["data-id"] = id;
                }
            }
        }

        protected void btnBuscarAlumno_OnClick(object sender, EventArgs e)
        {

            var lista = boAlumno.buscarAlumnos(
                txtCodFam_Alum.Text,
                txtApePat_Alum.Text,
                txtApeMat_Alum.Text,
                txtNombre_Alum.Text,
                txtdni_Alum.Text
            );

            gvAlumnoModal.DataSource = lista;
            gvAlumnoModal.DataBind();

            //Forzar que el UpdatePanel pinte el DOM nuevo
            upAlumno.Update();

            // Mantener el modal abierto DESDE el UpdatePanel (clave)
            ScriptManager.RegisterStartupScript(
                upAlumno,                 // <<— destino: el panel actualizado
                upAlumno.GetType(),
                "keepFamiliaOpen",
                "setTimeout(function(){var ov=document.getElementById('ovAlumno'); if(ov){ ov.classList.add('show'); ov.setAttribute('aria-hidden','false'); }},0);",
                true
            );

        }





        protected void gvAulaModal_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                e.Row.CssClass += " data-row";

                if (e.Row.RowIndex < gvAulaModal.DataKeys.Count)
                {
                    //var data = (periodoXAula)e.Row.DataItem;
                    //e.Row.Attributes["data-id"] = data.periodo_aula_id.ToString();
                    var id = gvAulaModal.DataKeys[e.Row.RowIndex].Value.ToString();
                    e.Row.Attributes["periodo_aula_id"] = id;
                }
            }

        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            // TODO: actualizar matrícula en BD con el Id actual.
            // Datos: hdnAlumnoId.Value, hdnAulaId.Value, txtActivo.Text, etc.

            matricula mat = new matricula();
            mat.matricula_id = idMatricula;
            mat.alumno = new alumno { alumno_id = int.Parse(hdnAlumnoId.Value) };
            periodoXAula periodo = new periodoXAula();
            periodo.periodo_aula_id = int.Parse(hdnAulaId.Value);
            mat.periodo_Aula = periodo;
            //mat.periodo_Aula.periodo.nombre = anio2.ToString();
            mat.activo = int.Parse(ddlActivo.SelectedValue);
            boMatricula.modificarMatricula(mat);
            //int anio = int.Parse((string)ViewState["anio"]);
            string modo = "consultar";

            Response.Redirect(ResolveUrl($"~/Matricula/BuscarMatricula.aspx?anio={anio2}&modo={modo}"));
        }

        protected void btnCancelar_Click(object sender, EventArgs e)
        {
            string modo = "consultar";

            Response.Redirect(ResolveUrl($"~/Matricula/BuscarMatricula.aspx?anio={anio2}&modo={modo}"));
        }
    }
}
