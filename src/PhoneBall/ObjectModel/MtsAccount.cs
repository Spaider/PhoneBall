using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Spaider.PhoneBall.ObjectModel
{
  public class MtsAccount
  {
    private Cookie _authCookie;
    private Cookie _sessionCookie;
    private readonly Regex _balanceRegex = new Regex(@"\<td\>\s*Актуальный баланс:\s*\</td\>\s*\<td\>\s*(?<Balance>[\d,\,]*).*\s*\<\/td\>", RegexOptions.Compiled);
    private const string _viewState = "dDwxMjQwMDQ3NzIxO3Q8O2w8aTwwPjtpPDE%2BO2k8Mj47PjtsPHQ8cDxsPGlubmVyaHRtbDs%2BO2w80JjQvdGC0LXRgNC90LXRgi3Qv9C%2B0LzQvtGJ0L3QuNC6Oz4%2BOzs%2BO3Q8O2w8aTwxPjs%2BO2w8dDxwPHA8bDxWaXNpYmxlOz47bDxvPHQ%2BOz4%2BOz47bDxpPDE%2BO2k8Mz47PjtsPHQ8cDxwPGw8VGV4dDs%2BO2w80YDRg9GB0YHQutC40Lk7Pj47Pjs7Pjt0PHA8cDxsPFRleHQ7PjtsPGVuZ2xpc2g7Pj47Pjs7Pjs%2BPjs%2BPjt0PDtsPGk8MT47aTwzPjtpPDc%2BO2k8MTE%2BO2k8MTU%2BO2k8MTc%2BO2k8MTg%2BOz47bDx0PHA8cDxsPFRleHQ7PjtsPNCa0L7QtDs%2BPjs%2BOzs%2BO3Q8cDxwPGw8VGV4dDs%2BO2w80J3QvtC80LXRgCDRgtC10LvQtdGE0L7QvdCwOjs%2BPjs%2BOzs%2BO3Q8dDxwPHA8bDxIZWlnaHQ7XyFTQjs%2BO2w8MTxcZT47aTwxMjg%2BOz4%2BOz47Oz47Oz47dDxwPHA8bDxUZXh0Oz47bDzQn9Cw0YDQvtC70Yw6Oz4%2BOz47Oz47dDxwPHA8bDxUZXh0Oz47bDzQstC%2B0LnRgtC4INCyINGB0LjRgdGC0LXQvNGDOz4%2BOz47Oz47dDxwPHA8bDxFcnJvck1lc3NhZ2U7PjtsPNCS0Ysg0LLQstC10LvQuCDQvdC10LrQvtGA0YDQtdC60YLQvdGL0Lkg0L3QvtC80LXRgFw8YnIgL1w%2BDQoJCQkJ0J3QtdC%2B0LHRhdC%2B0LTQuNC80L4g0LLQstC10YHRgtC4INC90L7QvNC10YAg0YLQtdC70LXRhNC%2B0L3QsCDQuNC3IDcg0YbQuNGE0YAsINC90LDQv9GA0LjQvNC10YAsIA0KCQkJCTc2NTQzMjEsINC4INC%2F0YDQuNC90LDQtNC70LXQttCw0YnQuNC5INGB0LXRgtC4INCc0KLQoS4NCgkJOz4%2BOz47Oz47dDxwPHA8bDxFcnJvck1lc3NhZ2U7PjtsPNCS0Ysg0LLQstC10LvQuCDQvdC10LrQvtGA0YDQtdC60YLQvdGL0Lkg0L3QvtC80LXRgFw8YnIgL1w%2BDQoJCQkJ0J3QtdC%2B0LHRhdC%2B0LTQuNC80L4g0LLQstC10YHRgtC4INC90L7QvNC10YAg0YLQtdC70LXRhNC%2B0L3QsCDQuNC3IDcg0YbQuNGE0YAsINC90LDQv9GA0LjQvNC10YAsIA0KCQkJCTc2NTQzMjEsINC4INC%2F0YDQuNC90LDQtNC70LXQttCw0YnQuNC5INGB0LXRgtC4INCc0KLQoS4NCgkJOz4%2BOz47Oz47Pj47Pj47Pg4exCBoblRcxveHwfi7vUjZPktY";

    public MtsAccount(string phone, string password) : this(phone, password, phone){}

    public MtsAccount(string phone, string password, string @alias)
    {
      if (string.IsNullOrEmpty(phone))
      {
        throw new ArgumentException("Не задан номер телефона");
      }
      if (string.IsNullOrEmpty(password))
      {
        throw new ArgumentException("Не задан пароль");
      }
      Password = password;
      Alias = alias;
      Phone = phone;
    }

    public string             Alias         { get; set; }
    public float              Balance       { get; set; }
    public string             ErrorMessage  { get; set; }
    public bool               IsError       { get; set; }
    public string             Password      { get; set; }
    public string             Phone         { get; set; }
    public CheckBalanceState  State         { get; set; }

    public void CheckBalance()
    {
      var req = (HttpWebRequest)WebRequest.Create("https://ihelper.mts.by");
      req.Method = "GET";
      req.ProtocolVersion = HttpVersion.Version11;
      req.CookieContainer = new CookieContainer();

      var resp = (HttpWebResponse)req.GetResponse();
      foreach (Cookie cookie in resp.Cookies)
      {
        if (cookie.Name != "ASP.NET_SessionId")
        {
          continue;
        }
        _sessionCookie = cookie;
        break;
      }
      resp.Close();

      req = (HttpWebRequest)WebRequest.Create("https://ihelper.mts.by/login.aspx");
      req.Method = "POST";
      req.Referer = "https://ihelper.mts.by/";
      req.AllowAutoRedirect = false;
      req.ContentType = "application/x-www-form-urlencoded";
      AddCookies(req);

      byte[] buffer = Encoding.ASCII.GetBytes(
                string.Format("__EVENTTARGET=loginLinkButton&__EVENTARGUMENT=&__VIEWSTATE={0}&DropDownList1=29&phoneNumberEdit={1}&passwordEdit={2}", 
                _viewState, 
                Phone, 
                Password));
      Stream requestStream = req.GetRequestStream();
      requestStream.Write(buffer, 0, buffer.Length);
      requestStream.Close();

      resp = (HttpWebResponse)req.GetResponse();
//      using(var sr = new StreamReader(resp.GetResponseStream()))
//      {
//        var ret = sr.ReadToEnd();
//      }
      foreach (Cookie cookie in resp.Cookies)
      {
        if (cookie.Name != ".ASPXAUTH")
        {
          continue;
        }
        _authCookie = cookie;
        break;
      }
      if (_authCookie == null || string.IsNullOrEmpty(_authCookie.Value))
      {
        IsError = true;
        ErrorMessage = "Неверный пароль";
        return;
      }
      resp.Close();

      if (resp.StatusCode != HttpStatusCode.Found)
      {
        IsError = true;
        ErrorMessage = "Последовательность проверки изменилась.";
        return;
      }

      if (resp.Headers["Location"].IndexOf("error") != -1)
      {
        ErrorMessage = "Технический сбой";
        IsError = true;
        return;
      }

      req = (HttpWebRequest)WebRequest.Create(string.Format("https://ihelper.mts.by{0}", resp.Headers["Location"]));
      req.Referer = "	https://ihelper.mts.by/";
      AddCookies(req);

      resp = (HttpWebResponse)req.GetResponse();
      resp.Close();

      req = (HttpWebRequest)WebRequest.Create("https://ihelper.mts.by/account-status.aspx");
      req.Referer = "https://ihelper.mts.by/login-wait.aspx";
      AddCookies(req);

      resp = (HttpWebResponse)req.GetResponse();
      using (var sr = new StreamReader(resp.GetResponseStream()))
      {
        var ret = sr.ReadToEnd();
        Balance = GetBalance(ret);
      }
      resp.Close();
    }

    private void AddCookies(HttpWebRequest request)
    {
      if (request.CookieContainer == null)
      {
        request.CookieContainer = new CookieContainer();
      }
      if (_authCookie != null)
      {
        request.CookieContainer.Add(_authCookie);
      }
      if (_sessionCookie != null)
      {
        request.CookieContainer.Add(_sessionCookie);
      }
    }

    private float GetBalance(string ret)
    {
      var match = _balanceRegex.Match(ret);
      var balanceString = match.Groups["Balance"].Value;

      float balance;
      return
        float.TryParse(balanceString, NumberStyles.Currency, new CultureInfo("ru-RU"), out balance)
          ? balance
          : 0;
    }
  }
}