<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="MQWeb.Default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
<meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <asp:Button ID="btnSend" runat="server" Text="SendMsg" OnClick="btnSend_Click" />
        </div>
    <div>
        <asp:Button ID="btnRec" runat="server" Text="Receive" OnClick="btnRec_Click" />
        <asp:TextBox ID="txt" runat="server"></asp:TextBox>
    </div>
    </form>
</body>
</html>
