using System.Data;
using Microsoft.Data.SqlClient;
using System.IO;
using System;
using System.Windows.Forms;

namespace GestorClientes
{
    public partial class FormGestionClientes : Form
    {
        private string connectionString;
        private DataSet ds;
        private SqlDataAdapter da;
        private BindingSource bs;

        public FormGestionClientes()
        {
            InitializeComponent();
            SetupDatabase();
            InitializeDataComponents();
            BindControls();
            SetupEvents();
        }

        private void InitializeDataComponents()
        {
            ds = new DataSet();
            da = new SqlDataAdapter("SELECT * FROM Clientes", connectionString);
            SqlCommandBuilder builder = new SqlCommandBuilder(da);
            da.Fill(ds, "Clientes");

            ds.Tables["Clientes"].PrimaryKey = new DataColumn[] { ds.Tables["Clientes"].Columns["Id"] };

            da.RowUpdated += new SqlRowUpdatedEventHandler(OnRowUpdated);

            bs = new BindingSource(ds, "Clientes");
            dgvClientes.DataSource = bs;
        }

        private void OnRowUpdated(object sender, SqlRowUpdatedEventArgs e)
        {
            if (e.StatementType == StatementType.Insert)
            {
                e.Status = UpdateStatus.Continue;
                using (SqlCommand cmd = new SqlCommand("SELECT SCOPE_IDENTITY()", e.Command.Connection))
                {
                    e.Row["Id"] = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        private void BindControls()
        {
            txtNombre.DataBindings.Add("Text", bs, "Nombre");
            txtDni.DataBindings.Add("Text", bs, "DNI");
            txtTelefono.DataBindings.Add("Text", bs, "Telefono");
        }

        private void SetupEvents()
        {
            btnNuevo.Click += btnNuevo_Click;
            btnGuardar.Click += btnGuardar_Click;
            btnBorrar.Click += btnBorrar_Click;

            txtDni.KeyPress += txtNumeric_KeyPress;
            txtTelefono.KeyPress += txtNumeric_KeyPress;
        }

        private void txtNumeric_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void btnNuevo_Click(object sender, EventArgs e)
        {
            bs.AddNew();
        }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text) || string.IsNullOrWhiteSpace(txtDni.Text) || string.IsNullOrWhiteSpace(txtTelefono.Text))
            {
                MessageBox.Show("Todos los campos son obligatorios.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                bs.EndEdit();
                da.Update(ds, "Clientes");
                ds.Tables["Clientes"].AcceptChanges();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar los cambios: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnBorrar_Click(object sender, EventArgs e)
        {
            try
            {
                bs.RemoveCurrent();
                da.Update(ds, "Clientes");
                ds.Tables["Clientes"].AcceptChanges();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al borrar el cliente: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetupDatabase()
        {
            string dbFileName = "ClientesDB.mdf";
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dbFileName);
            connectionString = $"Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename={dbPath};Integrated Security=True;Connect Timeout=30";

            if (!File.Exists(dbPath))
            {
                CreateDatabase(dbFileName, dbPath);
            }

            CreateTable();
        }

        private void CreateDatabase(string dbFileName, string dbPath)
        {
            string masterConnectionString = "Data Source=(LocalDB)\\MSSQLLocalDB;Integrated Security=True;Connect Timeout=30";
            using (var connection = new SqlConnection(masterConnectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"CREATE DATABASE {Path.GetFileNameWithoutExtension(dbFileName)} ON PRIMARY (NAME={Path.GetFileNameWithoutExtension(dbFileName)}, FILENAME='{dbPath}')";
                    command.ExecuteNonQuery();
                }
            }
        }

        private void CreateTable()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Clientes]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [dbo].[Clientes] (
                            [Id]       INT           IDENTITY (1, 1) NOT NULL,
                            [Nombre]   NVARCHAR (50) NOT NULL,
                            [DNI]      NVARCHAR (50) NOT NULL,
                            [Telefono] NVARCHAR (50) NOT NULL,
                            PRIMARY KEY CLUSTERED ([Id] ASC)
                        );
                    END";
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
