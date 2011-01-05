using System;
using System.Windows.Forms;
using Spaider.PhoneBall.ObjectModel;

namespace Spaider.PhoneBall.UI
{
  public partial class MainForm : Form
  {
    public MainForm()
    {
      InitializeComponent();
    }

    private void button1_Click(object sender, EventArgs e)
    {
      var account = new MtsAccount(tbPhone.Text.Trim(), tbPassword.Text.Trim());
      account.CheckBalance();
      textBox1.Text = account.IsError ? account.ErrorMessage : account.Balance.ToString("F2");
    }
  }
}
