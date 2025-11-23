using FrontTA.SisProgWS;
using System;
using System.ComponentModel;
using System.Web.UI;
using System.Web.UI.WebControls;
using alumno = FrontTA.SisProgWS.alumno;
using AlumnoWSClient = FrontTA.SisProgWS.AlumnoWSClient;
using periodoXAula = FrontTA.SisProgWS.periodoXAula;
using PeriodoXAulaWSClient = FrontTA.SisProgWS.PeriodoXAulaWSClient;
using MatriculaWSClient = FrontTA.SisProgWS.MatriculaWSClient;

namespace FrontTA.Matricula
{
    public partial class CrearMatricula : Page
    {
        private readonly MatriculaWSClient boMatricula = new MatriculaWSClient();
        private readonly AlumnoWSClient boAlumno = new AlumnoWSClient();
        private readonly PeriodoXAulaWSClient boPeriodo = new PeriodoXAulaWSClient();

        private int AnioMatricula
        {
            get => (int)(ViewState["AnioMatricula"] ?? DateTime.Now.Year);
            set => ViewState["AnioMatricula"] = value;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                int anio = DateTime.Now.Year;
                int.TryParse(Request.QueryString["anio"], out anio);
                AnioMatricula = anio;

                lblAnio.Text = anio.ToString();

                // ====== cargar alumnos modal ======
                var list = boAlumno.listarAlumnosTodos() ?? new alumno[0];
                gvAlumnoModal.DataSource = new BindingList<alumno>(list);
                gvAlumnoModal.DataBind();

                // ====== cargar aulas del año ======
                var list2 = boMatricula.listarPeriodoXAulasParaAsignarMatriculas() ?? new periodoXAula[0];
                BindingList<periodoXAula> aulas = new BindingList<periodoXAula>();

                foreach (periodoXAula per in list2)
                {
                    // asegurar periodo completo
                    periodoXAula full = boPeriodo.obtenerPeriodoXAulaPorId(per.periodo_aula_id);

                    if (full?.periodo != null && full.periodo.nombre == anio.ToString())
                        aulas.Add(full); // usamos el full para tener vacantes_ocupadas correctas
                }

                gvAulaModal.DataSource = aulas;
                gvAulaModal.DataBind();

                txtMonto.Text = "S/ 0.00";
                txtActivo.Text = "Sí";
            }
        }

        protected void gvAlumnoModal_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            e.Row.CssClass += " data-row";

            if (e.Row.RowIndex < gvAlumnoModal.DataKeys.Count)
            {
                var id = gvAlumnoModal.DataKeys[e.Row.RowIndex].Value.ToString();
                e.Row.Attributes["data-id"] = id;
            }
        }

        protected void gvAulaModal_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            e.Row.CssClass += " data-row";

            if (e.Row.RowIndex < gvAulaModal.DataKeys.Count)
            {
                var id = gvAulaModal.DataKeys[e.Row.RowIndex].Value.ToString();
                e.Row.Attributes["data-id"] = id;
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

            upAlumno.Update();

            ScriptManager.RegisterStartupScript(
                upAlumno,
                upAlumno.GetType(),
                "keepAlumnoOpen",
                "setTimeout(function(){var ov=document.getElementById('ovAlumno'); if(ov){ ov.classList.add('show'); ov.setAttribute('aria-hidden','false'); }},0);",
                true
            );
        }

        protected void btnConfirmar_Click(object sender, EventArgs e)
        {
            try
            {
                int alumnoId = 0;
                int aulaId = 0;

                // ====== OJO: estos son los IDs reales que llena tu JS ======
                int.TryParse(hdnAlumnoId.Value, out alumnoId);
                int.TryParse(hdnAulaId.Value, out aulaId);

                if (alumnoId <= 0 || aulaId <= 0)
                {
                    ScriptManager.RegisterStartupScript(this, GetType(), "err",
                        "alert('Seleccione un alumno y un aula.');", true);
                    return;
                }

                int periodoAulaId = boMatricula.registrarMatriculaConVacantes(alumnoId, aulaId);

                if (periodoAulaId > 0)
                {
                    ScriptManager.RegisterStartupScript(this, GetType(), "ok",
                        "alert('Matrícula registrada correctamente.');", true);

                    Response.Redirect($"~/Matricula/BuscarMatricula.aspx?anio={DateTime.Now.Year}&modo=agregar", false);
                    Context.ApplicationInstance.CompleteRequest();
                }
                else
                {
                    ScriptManager.RegisterStartupScript(this, GetType(), "err",
                        "alert('No se pudo registrar la matrícula o no hay vacantes disponibles.');", true);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error Crear Matricula: " + ex.Message);
                ScriptManager.RegisterStartupScript(this, GetType(), "err",
                    "alert('Ocurrió un error al registrar la matrícula.');", true);
            }
        }

        protected void btnCancelar_Click(object sender, EventArgs e)
        {
            string modo = (AnioMatricula == DateTime.Now.Year) ? "agregar" : "consultar";
            Response.Redirect(ResolveUrl($"~/Matricula/BuscarMatricula.aspx?anio={AnioMatricula}&modo={modo}"));
        }
    }
}
