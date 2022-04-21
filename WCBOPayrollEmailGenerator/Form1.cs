using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace WCBOPayrollEmailGenerator
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        string greeting = null;
        string month_absent = null;
        Dictionary<string, Dictionary<string, decimal>> reason_for_abcense = new Dictionary<string, Dictionary<string, decimal>>() {
            { "personal", new Dictionary<string, decimal>() { { "took", -1 }, { "balance", -1 } } },
            { "illness", new Dictionary<string, decimal>() { { "took", -1 }, { "balance", -1 } } }
        };
        decimal daily_pay_rate = -1;
        DateTime first_docked_pay = new DateTime();
        List<string> paydates = new List<string>();
        int pay_periods_docked = -1;
        decimal days_docked = -1;
        bool was_Error = false;
        DateTime start_date = new DateTime();
        DateTime end_date = new DateTime();

        private void Form1_Load(object sender, EventArgs e)
        {
            // Change Payrates
            cmbDailyPayRate.Items.Clear();
            cmbSuspensionDailyPayRate.Items.Clear();

            string FilePath = AppDomain.CurrentDomain.BaseDirectory;
            string ratePath = string.Format("{0}Resources\\payrates.txt", Path.GetFullPath(Path.Combine(FilePath, @"..\..\")));

            using (var sr = new StreamReader(ratePath, Encoding.UTF8))
            {
                while (sr.Peek() >= 0)
                {
                    var item = sr.ReadLine();
                    cmbDailyPayRate.Items.Add(item);
                    cmbSuspensionDailyPayRate.Items.Add(item);
                }
            }
            
            // Generate Paydates and Add to cmb
            ratePath = string.Format("{0}Resources\\paydates.txt", Path.GetFullPath(Path.Combine(FilePath, @"..\..\")));
            using (var sr = new StreamReader(ratePath, Encoding.UTF8))
            {
                while (sr.Peek() >= 0)
                {
                    paydates.Add(sr.ReadLine());
                }
            }

            int result = -1;
            DateTime now = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
            int paydatesIndex = 0;
            while (result < 0)
            {
                var nextDate = paydates.ToArray()[paydatesIndex].Split('/');
                DateTime nextPaydate = new DateTime(Convert.ToInt32(nextDate[2]), Convert.ToInt32(nextDate[0]), Convert.ToInt32(nextDate[1]));
                result = DateTime.Compare(nextPaydate, now);
                paydatesIndex++;
            }

            for (int i = paydatesIndex - 1; i < paydatesIndex + 4; i++)
            {
                var item = paydates.ElementAt(i);
                cmbFirstDockedPay.Items.Add(item);
                cmbSuspensionFirstDockedPay.Items.Add(item);
                cmbGarnishFirstGarnishedPay.Items.Add(item);
            }
            cmbFirstDockedPay.SelectedIndex = 0;
            cmbSuspensionFirstDockedPay.SelectedIndex = 0;

            // Set Month of Absence to Current Month
            cmbMonthOfAbsents.SelectedIndex = now.Month - 1;
        }

        private string GetGreeting(int hour)
        {
            switch (hour)
            {
                case int t when t <= 11:
                    return "Morning";
                case int t when t > 11 && t <= 16:
                    return "Afternoon";
                case int t when t > 16:
                    return "Evening";
                default:
                    return "";
            }
        }

        private string PluralDay(decimal d)
        {
            if (d > 1)
            {
                return "days";
            }
            else
            {
                return "day";
            }
        }

        private void btnGen_Click(object sender, EventArgs e)
        {
            rtbEmail.Clear();
            was_Error = false;
            lblDailyPayRate.ForeColor = Color.FromArgb(246, 236, 236);
            lblPersonal.ForeColor = Color.FromArgb(246, 236, 236);
            lblIllness.ForeColor = Color.FromArgb(246, 236, 236);

            // Greeting
            greeting = GetGreeting(DateTime.Now.Hour);

            // Month Absent
            month_absent = cmbMonthOfAbsents.Text;

            // Reason for Abcense
            reason_for_abcense["personal"]["took"] = nudPersonalTook.Value;
            reason_for_abcense["illness"]["took"] = nudIllnessTook.Value;
            reason_for_abcense["personal"]["balance"] = nudPersonalBalance.Value;
            reason_for_abcense["illness"]["balance"] = nudIllnessBalance.Value;

            days_docked = (nudPersonalTook.Value + nudIllnessTook.Value) - (nudPersonalBalance.Value + nudIllnessBalance.Value);

            // Daily Payrate
            pay_periods_docked = Convert.ToInt32(nudPayPeriodsDocked.Value);
            try
            {
                daily_pay_rate = Convert.ToDecimal(cmbDailyPayRate.Text.Split(' ').Last());
            }
            catch
            {
                was_Error = true;
                lblDailyPayRate.ForeColor = Color.Red;
            }

            // First Docked Pay
            first_docked_pay = new DateTime(Convert.ToInt32(cmbFirstDockedPay.Text.Split('/')[2]), Convert.ToInt32(cmbFirstDockedPay.Text.Split('/')[0]), Convert.ToInt32(cmbFirstDockedPay.Text.Split('/')[1]));

            // Greeting
            rtbEmail.Text += $"Good {greeting},\n\n";

            // Days Abcent
            if (reason_for_abcense["personal"]["took"] > 0 && reason_for_abcense["illness"]["took"] > 0)
            {
                rtbEmail.Text += $"I have been advised that you were absent from work {reason_for_abcense["personal"]["took"]} {PluralDay(reason_for_abcense["personal"]["took"])} for personal reasons and {reason_for_abcense["illness"]["took"]} {PluralDay(reason_for_abcense["illness"]["took"])} for illness reasons ";
            }
            else if (reason_for_abcense["personal"]["took"] > 0)
            {
                rtbEmail.Text += $"I have been advised that you were absent from work {reason_for_abcense["personal"]["took"]} {PluralDay(reason_for_abcense["personal"]["took"])} for personal reasons ";
            }
            else if (reason_for_abcense["illness"]["took"] > 0)
            {
                rtbEmail.Text += $"I have been advised that you were absent from work {reason_for_abcense["illness"]["took"]} {PluralDay(reason_for_abcense["illness"]["took"])} for illness reasons ";
            }
            else
            {
                lblPersonal.ForeColor = Color.Red;
                lblIllness.ForeColor = Color.Red;
                was_Error = true;
            }

            // Month And Year Of Absents
            rtbEmail.Text += $"in the month of {month_absent} {DateTime.Now.Year}. ";

            // Returning Balance
            // Took Both
            if (reason_for_abcense["personal"]["took"] > 0 && reason_for_abcense["illness"]["took"] > 0)
            {
                // Balance of Both
                if (reason_for_abcense["personal"]["balance"] > 0 && reason_for_abcense["illness"]["balance"] > 0)
                {
                    rtbEmail.Text += $"Since you only have a remaining balance of {reason_for_abcense["personal"]["balance"]} {PluralDay(reason_for_abcense["personal"]["balance"])} for personal reasons and {reason_for_abcense["illness"]["balance"]} {PluralDay(reason_for_abcense["illness"]["balance"])} for illness reasons, ";
                }
                // Balance of Personal No Illness
                else if (reason_for_abcense["personal"]["balance"] > 0)
                {
                    rtbEmail.Text += $"Since you only have a remaining balance of {reason_for_abcense["personal"]["balance"]} {PluralDay(reason_for_abcense["personal"]["balance"])} for personal reasons and have no more remaining days for illness reasons, ";
                }
                // Balance of Illness No Personal
                else if (reason_for_abcense["illness"]["balance"] > 0)
                {
                    rtbEmail.Text += $"Since you only have a remaining balance of {reason_for_abcense["illness"]["balance"]} {PluralDay(reason_for_abcense["illness"]["balance"])} for illness reasons and have no more remaining days for personal reasons, ";
                }
            }
            // Took Personal
            else if (reason_for_abcense["personal"]["took"] > 0)
            {
                // Balance Of Personal
                if (reason_for_abcense["personal"]["balance"] > 0)
                {
                    rtbEmail.Text += $"Since you only have a remaining balance of {reason_for_abcense["personal"]["balance"]} {PluralDay(reason_for_abcense["personal"]["balance"])} for personal reasons, ";
                }
                // No Balance Of Personal
                else
                {
                    rtbEmail.Text += $"Since you do not have any more personal day's remaining, ";
                }
            }
            // Took Illness
            else if (reason_for_abcense["illness"]["took"] > 0)
            {
                // Balance Of Illness
                if (reason_for_abcense["illness"]["balance"] > 0)
                {
                    rtbEmail.Text += $"Since you only have a remaining balance of {reason_for_abcense["illness"]["balance"]} {PluralDay(reason_for_abcense["illness"]["balance"])} for personal reasons, ";
                }
                // No Balance Of Illness
                else
                {
                    rtbEmail.Text += $"Since you do not have any more illness days remaining, ";
                }
            }

            rtbEmail.Text += $"that will be a dock of {days_docked} {PluralDay(days_docked)}.";

            // Pay Reduction
            if (pay_periods_docked is 1)
            {
                rtbEmail.Text += $"\n\nYou will see your gross pay reduced by ${String.Format("{0:0.00}", days_docked * daily_pay_rate)} ({days_docked} {PluralDay(days_docked)} of pay) on {first_docked_pay.ToString("MMMM")} {first_docked_pay.Day}, {first_docked_pay.Year} payroll.";
            }
            else if (pay_periods_docked is 2)
            {
                List<double> docked_ammounts = new List<double>();
                if (Math.Floor(days_docked) != days_docked)
                {
                    docked_ammounts.Add((Convert.ToDouble(Math.Floor(days_docked)) / 2) + 0.5);
                }
                else
                {
                    docked_ammounts.Add(Convert.ToDouble(Math.Floor(days_docked)) / 2);
                }
                docked_ammounts.Add(Convert.ToDouble(Math.Floor(days_docked)) / 2);

                rtbEmail.Text += $"\n\nAs a courtesy, we will split these docked days over {pay_periods_docked} pays. " +
                    $"You will see your gross pay reduced by ${String.Format("{0:0.00}", docked_ammounts.ElementAt(0) * Convert.ToDouble(daily_pay_rate))} ({docked_ammounts.ElementAt(0)} {PluralDay(Convert.ToDecimal(docked_ammounts.ElementAt(0)))} of pay) on {first_docked_pay.ToString("MMMM")} {first_docked_pay.Day}, {first_docked_pay.Year} payroll. " +
                    $"Your gross pay will also be reduced by ${String.Format("{0:0.00}", docked_ammounts.ElementAt(1) * Convert.ToDouble(daily_pay_rate))} ({docked_ammounts.ElementAt(1)} {PluralDay(Convert.ToDecimal(docked_ammounts.ElementAt(1)))} of pay) on {first_docked_pay.AddDays(14).ToString("MMMM")} {first_docked_pay.AddDays(14).Day}, {first_docked_pay.AddDays(14).Year} payroll.";
            }
            else
            {
                List<decimal> docked_ammounts = new List<decimal>();
                docked_ammounts.Add(Math.Ceiling((days_docked / 3) / .5m) * .5m);
                docked_ammounts.Add(Math.Ceiling(((days_docked - docked_ammounts.ElementAt(0)) / 2) / .5m) * .5m);
                docked_ammounts.Add(days_docked - docked_ammounts.ElementAt(0) - docked_ammounts.ElementAt(1));

                rtbEmail.Text += $"\n\nAs a courtesy, we will split these docked days over {pay_periods_docked} pays. " +
                    $"You will see your gross pay reduced by ${String.Format("{0:0.00}", docked_ammounts.ElementAt(0) * Convert.ToDecimal(daily_pay_rate))} ({docked_ammounts.ElementAt(0)} {PluralDay(Convert.ToDecimal(docked_ammounts.ElementAt(0)))} of pay) on {first_docked_pay.ToString("MMMM")} {first_docked_pay.Day}, {first_docked_pay.Year} payroll. " +
                    $"Your gross pay will also be reduced by ${String.Format("{0:0.00}", docked_ammounts.ElementAt(1) * Convert.ToDecimal(daily_pay_rate))} ({docked_ammounts.ElementAt(1)} {PluralDay(Convert.ToDecimal(docked_ammounts.ElementAt(1)))} of pay) on {first_docked_pay.AddDays(14).ToString("MMMM")} {first_docked_pay.AddDays(14).Day}, {first_docked_pay.AddDays(14).Year} payroll. " +
                    $"You will also see your gross pay reduced by ${ String.Format("{0:0.00}", docked_ammounts.ElementAt(2) * Convert.ToDecimal(daily_pay_rate))} ({docked_ammounts.ElementAt(2)} {PluralDay(Convert.ToDecimal(docked_ammounts.ElementAt(2)))} of pay) on {first_docked_pay.AddDays(28).ToString("MMMM")} {first_docked_pay.AddDays(28).Day}, {first_docked_pay.AddDays(28).Year} payroll.";
            }

            rtbEmail.Text += "\n\nPlease let me know if you have any questions.";

            // At End If There Was Error No Show Half Made Email
            if (was_Error)
            {
                rtbEmail.Clear();
            }
        }
        
        private void btnSuspensionGen_Click(object sender, EventArgs e)
        {
            was_Error = false;
            lblSuspensionDailyPayRate.ForeColor = Color.FromArgb(246, 236, 236);

            rtbEmail.Clear();
            // Get greeting
            greeting = GetGreeting(DateTime.Now.Hour);

            // Get Start and End Date
            start_date = dtpSuspensionStart.Value;
            end_date = dtpSuspensionEnd.Value;

            // Payrate & First Docked Pay
            try
            {
                daily_pay_rate = Convert.ToDecimal(cmbSuspensionDailyPayRate.Text.Split(' ').Last());
            }
            catch
            {
                lblSuspensionDailyPayRate.ForeColor = Color.Red;
                was_Error = true;
            }
            first_docked_pay = new DateTime(Convert.ToInt32(cmbSuspensionFirstDockedPay.Text.Split('/')[2]), Convert.ToInt32(cmbSuspensionFirstDockedPay.Text.Split('/')[0]), Convert.ToInt32(cmbSuspensionFirstDockedPay.Text.Split('/')[1]));

            // Days Suspended
            int days_suspended = 1;
            if (chbSuspensionEnd.Checked)
            {
                days_suspended = Convert.ToInt32(end_date.Subtract(start_date).TotalDays) + 1;
            }

            rtbEmail.Text += $"Good {greeting}!" +
                $"\n\nI have been advised that you were suspended without pay for {days_suspended} {PluralDay(Convert.ToDecimal(days_suspended))} ";
            
            if (chbSuspensionEnd.Checked)
            {
                rtbEmail.Text += $"from {start_date.ToString("MM/dd/yyyy")} - {end_date.ToString("MM/dd/yyyy")}";
            }
                
            rtbEmail.Text += $". This will result in a dock of {days_suspended} {PluralDay(Convert.ToDecimal(days_suspended))} pay." +
                $"\n\nYou will see your gross pay reduced by ${String.Format("{0:0.00}", Convert.ToDouble(daily_pay_rate) * days_suspended)} ({days_suspended} {PluralDay(Convert.ToDecimal(days_suspended))} of pay) on the {first_docked_pay.ToString("MMMM dd")}, {first_docked_pay.Year} payroll." +
                $"\n\nPlease ley me know if you have any questions.";

            // At End If There Was Error No Show Half Made Email
            if (was_Error)
            {
                rtbEmail.Clear();
            }
        }

        private void cmbSuspensionEnd_CheckedChanged(object sender, EventArgs e)
        {
            if (chbSuspensionEnd.Checked)
            {
                dtpSuspensionEnd.Enabled = true;
            }
            else
            {
                dtpSuspensionEnd.Enabled = false;
            }
        }

        private void btnGarnishGen_Click_1(object sender, EventArgs e)
        {
            rtbEmail.Clear();
            was_Error = false;
            lblGarnishmentFirstGarnishedPay.ForeColor = Color.FromArgb(246, 236, 236);

            // Greeting
            greeting = GetGreeting(DateTime.Now.Hour);

            // Amount Garnished & Percent Garnished & First Garnished Pay
            double amount_garnished = Convert.ToDouble(nudGarnishAmountGarnished.Value);
            double percent_garnished = Convert.ToDouble(nudGarnishPercentGarnished.Value);
            DateTime first_garnished_pay = new DateTime();
            try
            {
                first_garnished_pay = new DateTime(Convert.ToInt32(cmbGarnishFirstGarnishedPay.Text.Split('/')[2]), Convert.ToInt32(cmbGarnishFirstGarnishedPay.Text.Split('/')[0]), Convert.ToInt32(cmbGarnishFirstGarnishedPay.Text.Split('/')[1]));
            }
            catch
            {
                was_Error = true;
                lblGarnishmentFirstGarnishedPay.ForeColor = Color.Red;
            }

            // Writing Email
            rtbEmail.Text += $"Good {greeting}!" +
                $"\n\nI have been advised that there is an Order of Garnishment for you payable to Warrick Circuit and Superior Courts, cause # 87D01-xxxx-SC-xxx " +
                $"in the amount of ${amount_garnished}. We will begin garnishing {percent_garnished}% of your disposable income to satisfy these orders on the {first_garnished_pay.ToString("MM/dd/yyyy")} pay." +
                $"\n\nIf you hvae any questions, please let me know.";

            // At End If There Was Error No Show Half Made Email
            if (was_Error)
            {
                rtbEmail.Clear();
            }
        }

        private void btnJuryGen_Click_1(object sender, EventArgs e)
        {
            rtbEmail.Clear();
            was_Error = false;

            // Greeting
            greeting = GetGreeting(DateTime.Now.Hour);

            // Setting Vars
            DateTime day_payed = dtpJuryDayPayed.Value;
            double amount_docked = 40;
            start_date = dtpJuryStart.Value;
            end_date = dtpJuryEnd.Value;

            if (chbJuryEnd.Checked)
            {
                amount_docked = (end_date.Subtract(start_date).TotalDays + 1) * 40;
            }

            // Writing Email
            rtbEmail.Text += $"Good {greeting}!" +
                $"\n\nI wanted to touch base with your regarding your pay on {day_payed.ToString("MMMM dd")}, {day_payed.Year}. " +
                $"You will be docked {String.Format("{0:0.00}", amount_docked)} for the money you recived from the courts for Jury Duty on {start_date.ToString("MM/dd/yyyy")}";

            // Checking if there are multiplue days or not
            if (chbJuryEnd.Checked)
            {
                rtbEmail.Text += $" to {end_date.ToString("MM/dd/yyyy")}";
            }
            rtbEmail.Text += $".\n\nIf you have any questions or concerns, please let me know.";


            // At End If There Was Error No Show Half Made Email
            if (was_Error)
            {
                rtbEmail.Clear();
            }
        }

        private void checkBox1_CheckedChanged_1(object sender, EventArgs e)
        {
            if (chbJuryEnd.Checked)
            {
                dtpJuryEnd.Enabled = true;
            }
            else
            {
                dtpJuryEnd.Enabled = false;
            }
        }

        #region TopBar
        private void pbMinimise_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void pbClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        bool mouseDown;
        Point lastLocation;

        private void panTopBar_MouseDown(object sender, MouseEventArgs e)
        {
            mouseDown = true;
            lastLocation = e.Location;
        }

        private void panTopBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseDown)
            {
                this.Location = new Point(
                    (this.Location.X - lastLocation.X) + e.X, (this.Location.Y - lastLocation.Y) + e.Y);

                this.Update();
            }
        }

        private void panTopBar_MouseUp(object sender, MouseEventArgs e)
        {
            mouseDown = false;
        }
        #endregion

        #region TabbedPain

        private void bunifuButton3_Click(object sender, EventArgs e)
        {
            panJuryDuty.Location = new Point(0, 74);
            panSuspension.Location = new Point(926, 518);
            panAbsent.Location = new Point(2000, 1000);
            panGarnishment.Location = new Point(1449, 0);
        }

        private void btnAbsent_Click(object sender, EventArgs e)
        {
            panAbsent.Location = new Point(0, 43);
            panSuspension.Location = new Point(926, 518);
            panJuryDuty.Location = new Point(1458, 436);
            panGarnishment.Location = new Point(1449, 0);
        }

        private void btnSuspension_Click(object sender, EventArgs e)
        {
            panSuspension.Location = new Point(0, 74);
            panAbsent.Location = new Point(2000, 1000);
            panJuryDuty.Location = new Point(1458, 436);
            panGarnishment.Location = new Point(1449, 0);
        }

        private void btnGarnishment_Click(object sender, EventArgs e)
        {
            panGarnishment.Location = new Point(0, 74);
            panAbsent.Location = new Point(2000, 1000);
            panJuryDuty.Location = new Point(1458, 436);
            panSuspension.Location = new Point(926, 518);
        }

        #endregion

        #region rtb Ease of Use

        private void pbCopy_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(rtbEmail.Text);
            }
            catch
            {

            }
        }

        private void pbClear_Click(object sender, EventArgs e)
        {
            rtbEmail.Clear();
            nudPersonalBalance.Value = 0;
            nudPersonalTook.Value = 0;
            nudIllnessBalance.Value = 0;
            nudIllnessTook.Value = 0;
            nudPayPeriodsDocked.Value = 1;
            cmbDailyPayRate.Text = "";
            nudGarnishAmountGarnished.Value = 0;
            nudGarnishPercentGarnished.Value = 0;
            dtpSuspensionEnd.Value = DateTime.Now;
            dtpSuspensionStart.Value = DateTime.Now;
            chbSuspensionEnd.Checked = false;
            cmbSuspensionDailyPayRate.Text = "";
            dtpJuryDayPayed.Value = DateTime.Now;
            dtpJuryEnd.Value = DateTime.Now;
            dtpJuryStart.Value = DateTime.Now;
            chbJuryEnd.Checked = false;
        }

        #endregion

        #region i did a boo boo

        private void cmbMonthOfAbsents_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void rtbEmail_TextChanged(object sender, EventArgs e)
        {

        }

        private void tbcMain_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void tbpBasic_Click(object sender, EventArgs e)
        {

        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void nudIllnessBalance_ValueChanged(object sender, EventArgs e)
        {

        }

        private void nudPersonalBalance_ValueChanged(object sender, EventArgs e)
        {

        }

        private void lblMonthOfAbsents_Click(object sender, EventArgs e)
        {

        }

        private void lblReasonForAbsents_Click(object sender, EventArgs e)
        {

        }

        private void lblPersonal_Click(object sender, EventArgs e)
        {

        }

        private void cmbFirstDockedPay_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void lblDailyPayRate_Click(object sender, EventArgs e)
        {

        }

        private void lblPayPeriodsDocked_Click(object sender, EventArgs e)
        {

        }

        private void nudPersonalTook_ValueChanged(object sender, EventArgs e)
        {

        }

        private void cmbDailyPayRate_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void lblFirstDockedPayDay_Click(object sender, EventArgs e)
        {

        }

        private void lblIllness_Click(object sender, EventArgs e)
        {

        }

        private void nudPayPeriodsDocked_ValueChanged(object sender, EventArgs e)
        {

        }

        private void nudIllnessTook_ValueChanged(object sender, EventArgs e)
        {

        }

        private void tbpSuspension_Click(object sender, EventArgs e)
        {

        }

        private void cmbSuspensionDailyPayRate_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void cmbSuspensionFirstDockedPay_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void dtpSuspensionEnd_ValueChanged(object sender, EventArgs e)
        {

        }

        private void dtpSuspensionStart_ValueChanged(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void tbpJuryDuty_Click(object sender, EventArgs e)
        {

        }

        private void dtpJuryEnd_ValueChanged(object sender, EventArgs e)
        {

        }

        private void dtpJuryStart_ValueChanged(object sender, EventArgs e)
        {

        }

        private void label11_Click(object sender, EventArgs e)
        {

        }

        private void dtpJuryDayPayed_ValueChanged(object sender, EventArgs e)
        {

        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void tbpGarnishment_Click(object sender, EventArgs e)
        {

        }

        private void nudGarnishPercentGarnished_ValueChanged(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void nudGarnishAmountGarnished_ValueChanged(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void cmbGarnishFirstGarnishedPay_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void panTopBar_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panAbsent_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panSuspension_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panJuryDuty_Paint(object sender, PaintEventArgs e)
        {

        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {

        }

        private void dateTimePicker2_ValueChanged(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void dateTimePicker3_ValueChanged(object sender, EventArgs e)
        {

        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        private void panGarnishment_Paint(object sender, PaintEventArgs e)
        {

        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {

        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {

        }

        private void label12_Click(object sender, EventArgs e)
        {

        }

        private void label13_Click(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        #endregion

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {

        }
    }
}
