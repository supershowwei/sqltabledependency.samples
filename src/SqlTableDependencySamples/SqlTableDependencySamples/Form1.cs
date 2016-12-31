using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Transactions;
using System.Windows.Forms;
using Newtonsoft.Json;
using SqlTableDependencySamples.Extensions;
using SqlTableDependencySamples.Model.Data;
using TableDependency.Enums;
using TableDependency.Mappers;
using TableDependency.SqlClient;

namespace SqlTableDependencySamples
{
    public partial class Form1 : Form
    {
        private static readonly string ConnectionString =
            File.ReadAllText(
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "connectionstring.txt"));

        private SqlTableDependency<Customer> customerDependency;

        private SqlTableDependency<Employee> employeeDependency;

        private SqlTableDependency<MyDependencyTable> myDependencyTableDependency;

        public Form1()
        {
            this.InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Model and properties with same name of table and columns
            this.myDependencyTableDependency = new SqlTableDependency<MyDependencyTable>(ConnectionString);

            this.myDependencyTableDependency.OnChanged +=
                (o, args) => this.OutputNotification(args.ChangeType, args.Entity);

            this.myDependencyTableDependency.Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Code First Data Annotations to map model with database table
            this.customerDependency = new SqlTableDependency<Customer>(ConnectionString);

            this.customerDependency.OnChanged += (o, args) => this.OutputNotification(args.ChangeType, args.Entity);
            this.customerDependency.Start();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // Custom map between model property and table column using ModelToTableMapper<T>
            var mapper = new ModelToTableMapper<Employee>();
            mapper.AddMapping(c => c.No, "Id");
            mapper.AddMapping(c => c.Detail, "Description");

            this.employeeDependency = new SqlTableDependency<Employee>(ConnectionString, "MyDependencyTable", mapper);

            this.employeeDependency.OnChanged += (o, args) => this.OutputNotification(args.ChangeType, args.Entity);
            this.employeeDependency.Start();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // Specify for which properties we want receive notifications using IList<string>
            this.myDependencyTableDependency = new SqlTableDependency<MyDependencyTable>(
                                                   ConnectionString,
                                                   new List<string> { "Name" });

            this.myDependencyTableDependency.OnChanged +=
                (o, args) => this.OutputNotification(args.ChangeType, args.Entity);

            this.myDependencyTableDependency.Start();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            // Specify for which properties we want receive notification using UpdateOfModel<T> mapper
            var updateOfModel = new UpdateOfModel<MyDependencyTable>();
            updateOfModel.Add(i => i.Name);

            this.myDependencyTableDependency = new SqlTableDependency<MyDependencyTable>(
                                                   ConnectionString,
                                                   updateOfModel);

            this.myDependencyTableDependency.OnChanged +=
                (o, args) => this.OutputNotification(args.ChangeType, args.Entity);

            this.myDependencyTableDependency.Start();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            // Filter notification by operation type
            this.myDependencyTableDependency = new SqlTableDependency<MyDependencyTable>(
                                                   ConnectionString,
                                                   nameof(MyDependencyTable),
                                                   null,
                                                   (IList<string>)null,
                                                   DmlTriggerType.Delete | DmlTriggerType.Insert);

            this.myDependencyTableDependency.OnChanged +=
                (o, args) => this.OutputNotification(args.ChangeType, args.Entity);

            this.myDependencyTableDependency.Start();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            var dt = new DataTable();
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("Name", typeof(string));
            dt.Columns.Add("Description", typeof(string));

            for (var i = 0; i < 100000; i++)
            {
                var row = dt.NewRow();
                row["Id"] = i;
                row["Name"] = "MyName " + i;
                row["Description"] = Guid.NewGuid().ToString();

                dt.Rows.Add(row);
            }

            using (var tx = new TransactionScope())
            {
                using (var sql = new SqlConnection(ConnectionString))
                {
                    sql.Open();

                    using (var sqlBulkCopy = new SqlBulkCopy(sql))
                    {
                        sqlBulkCopy.DestinationTableName = "dbo.MyDependencyTable";
                        sqlBulkCopy.WriteToServer(dt);
                    }
                }

                tx.Complete();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.myDependencyTableDependency?.Stop();
            this.customerDependency?.Stop();
            this.employeeDependency?.Stop();
        }

        private void OutputNotification(ChangeType changeType, object entity)
        {
            this.textBox1.InvokeIfNecessary(
                () => { this.textBox1.AppendText($"{changeType}, {JsonConvert.SerializeObject(entity)}\r\n"); });
        }
    }
}