<?php
include("databaseinfo.php");

mysql_connect ($DB_HOST, $DB_USER, $DB_PASSWORD);
mysql_select_db ($DB_NAME);

$task = $_REQUEST['task'];

$page = isset($_REQUEST['page']) ? $_REQUEST['page'] : 1;

$itemsPerPage = 500;

$limit = "LIMIT ".$itemsPerPage;
if ($page>1) $limit = "LIMIT ".(($page-1)*$itemsPerPage).", ".$itemsPerPage;

switch ($task)
{
    default:
        dwellHtml::showIndex();
        break;
    case 'showRegions':
        $sql = "SELECT region_name, region_uuid,DATE(timestamp) AS ddate, count(*) AS total FROM landDwell GROUP BY region_uuid,ddate ORDER BY ddate DESC ".$limit;
        $result = mysql_query($sql);
        $data = array();
        while ($row = mysql_fetch_assoc($result)) {
            $data[] = $row;
        }
        dwellHtml::showRegions($data,$page,$itemsPerPage);
        break;
    case 'showAvatar':
        $avatar = $_REQUEST['name'];
        $sql = "SELECT *, DATE(timestamp) AS ddate, TIME(timestamp) AS dtime FROM landDwell WHERE (id LIKE '".$avatar."' OR avatar_name LIKE '".$avatar."') ORDER BY timestamp DESC ".$limit;
        $result = mysql_query($sql);
        $data = array();
        while ($row = mysql_fetch_assoc($result)) {
            $data[] = $row;
        }
        dwellHtml::showAvatar($data,$page,$itemsPerPage);
        break;
    case 'showParcel':
        $parcel = $_REQUEST['name'];
        $sql = "SELECT pid,parcel_name,region_name,region_uuid,localLandID, parcelOwner, parcelGroupOwned, ParcelOwnerName, count(*) AS total,HOUR(timestamp) AS dhour, DAY(timestamp) AS dday, WEEK(timestamp) AS dweek, MONTH(timestamp) AS dmonth, DATE(timestamp) AS ddate FROM landDwell WHERE (pid LIKE '".$parcel."' OR parcel_name LIKE '".$parcel."' OR parcelOwner LIKE '".$parcel."' OR parcelOwnerName LIKE '".$parcel."') GROUP BY dhour,dday,dweek,dmonth ORDER BY timestamp DESC ".$limit;
        $result = mysql_query($sql);
        $data = array();
        while ($row = mysql_fetch_assoc($result)) {
            $data[] = $row;
        }
        dwellHtml::showParcel($data,$page,$itemsPerPage);       
        break;
    case 'showAvatarsOnParcel':
        $parcel = $_REQUEST['name'];
        $date = $_REQUEST['date'];
        $hour = isset($_REQUEST['hour']) ? $_REQUEST['hour'] :0;
        if ($hour>0) 
            $sql = "SELECT pid,parcel_name,region_name,region_uuid,localLandID, parcelOwner, parcelGroupOwned, ParcelOwnerName, avatar_name, id, HOUR(timestamp) AS dhour, DATE(timestamp) AS ddate, TIME(timestamp) AS dtime FROM landDwell WHERE (pid LIKE '".$parcel."' OR parcel_name LIKE '".$parcel."' OR parcelOwner LIKE '".$parcel."' OR parcelOwnerName LIKE '".$parcel."') AND DATE(timestamp) = '".$date."' AND HOUR(timestamp) = '".$hour."' ORDER BY timestamp DESC ".$limit;
        else
            $sql = "SELECT pid,parcel_name,region_name,region_uuid,localLandID, parcelOwner, parcelGroupOwned, ParcelOwnerName, avatar_name, id, HOUR(timestamp) AS dhour, DATE(timestamp) AS ddate, TIME(timestamp) AS dtime  FROM landDwell WHERE (pid LIKE '".$parcel."' OR parcel_name LIKE '".$parcel."' OR parcelOwner LIKE '".$parcel."' OR parcelOwnerName LIKE '".$parcel."') AND DATE(timestamp) = '".$date."' ORDER BY timestamp DESC ".$limit;
        $result = mysql_query($sql);
        $data = array();
        while ($row = mysql_fetch_assoc($result)) {
            $data[] = $row;
        }
        $extra = "&date=".$date;
        if ($hour>0) $extra .= "&hour=".$hour;
        dwellHtml::showAvatarsOnParcel($data,$page,$itemsPerPage,$extra);         
        break;
    case 'showRegion':
        $region = $_REQUEST['name'];
        $sql = "SELECT pid,parcel_name,region_name,region_uuid,localLandID, parcelOwner, parcelGroupOwned, ParcelOwnerName, count(*) AS total, DAY(timestamp) AS dday, DATE(timestamp) as ddate FROM landDwell WHERE (region_name LIKE '".$region."' OR region_uuid LIKE '".$region."') GROUP BY parcelOwner,dday,parcelGroupOwned ORDER BY timestamp DESC ".$limit;
        $result = mysql_query($sql);
        $data = array();
        while ($row = mysql_fetch_assoc($result)) {
            $data[] = $row;
        }
        dwellHtml::showRegion($data,$page,$itemsPerPage);        
        break;
        
}

class dwellHtml
{
    function showIndex()
    {
        ?>
        <html><body>
            <h2>Parcel Traffic</h2>
            <table border="0">
                <tr><td>
                    <a href="index.php?task=showRegions">Show regions traffic</a>
                </td></tr>
                <tr><td>
                    <form action="index.php?task=showAvatar" method="post">
                        Avatar: <input type="text" name="name"> (the name or uuid of avatar)
                        <input type="submit">
                    </form>
                </td></tr>
                 <tr><td>
                    <form action="index.php?task=showParcel" method="post">
                        Parcel: <input type="text" name="name"> (the name of the parcel, the owner/group uuid or name)
                        <input type="submit">
                    </form>
                </td></tr>
                 <tr><td>
                    <form action="index.php?task=showRegion" method="post">
                        Region: <input type="text" name="name"> (the name or uuid of the region)
                        <input type="submit">
                    </form>
                </td></tr>                  
            </table>
            
        </body></html>
        <?php
    }
 
    function showRegions($data,$page,$itemsPerPage)
    {
        echo "<html><body><table border=\"0\" cellpadding=\"5\"><th>Region name</th><th>Region uuid</th><th>date</th><th>total visitors</th>";
        $bgrow = -1;
        foreach ($data AS $dat)
        {
            $background = "#F0F0F0";
            if ($bgrow==1) $background = "#C0C0C0";
            $bgrow = $bgrow * -1;
            echo "<tr bgcolor=\"".$background."\"><td><a href=\"index.php?task=showRegion&name=".$dat['region_uuid']."\">".$dat['region_name']."</a></td><td>".$dat['region_uuid']."</td><td>".$dat['ddate']."</td><td>".$dat['total']."</td></tr>";
        }
        echo "</table>";
        
        for ($i=1; $i<$page; $i++) { 
            echo "<a href=\"index.php?task=showRegions&page=".$i."\"> ".$i." </a> "; 
        };
        if (count($data)==$itemsPerPage) echo " <a href=\"index.php?task=showRegions&page=".($page+1)."\">next</a> "; 
        
        echo "<br><button onclick=\"window.history.back()\">Go Back</button>";
        echo "</body></html>"; 
    }
    
    function showRegion($data,$page,$itemsPerPage)
    {
        echo "<html><body><table border=\"0\" cellpadding=\"5\"><th>Region name</th><th>Parcel name</th><th>Parcel uuid</th><th>Owner</th><th>date</th><th>total visitors</th>";
        $bgrow = -1;
        foreach ($data AS $dat)
        {
            $background = "#F0F0F0";
            if ($bgrow==1) $background = "#C0C0C0";
            $bgrow = $bgrow * -1;
            $group = "";
            if ($dat['parcelGroupOwned']) $group = " (group)";
            echo "<tr bgcolor=\"".$background."\"><td>".$dat['region_name']."</td><td><a href=\"index.php?task=showParcel&name=".$dat['pid']."\">".$dat['parcel_name']."</a></td><td>".$dat['pid']."</td><td>".$dat['ParcelOwnerName'].$group."</td><td>".$dat['ddate']."</td><td>".$dat['total']."</td></tr>";
        }
        echo "</table>";
        for ($i=1; $i<$page; $i++) { 
            echo "<a href=\"index.php?task=showRegion&name=".$dat['region_uuid']."&page=".$i."\"> ".$i." </a> "; 
        };
        if (count($data)==$itemsPerPage) echo " <a href=\"index.php?task=showRegion&name=".$data[0]['region_uuid']."&page=".($page+1)."\">next</a> "; 
        
        echo "<br><button onclick=\"window.history.back()\">Go Back</button>";
        echo "</body></html>"; 
    }
    
    function showAvatar($data,$page,$itemsPerPage)
    {
        echo "<html><body><table border=\"0\" cellpadding=\"5\"><th>Avatar name</th><th>Avatar uuid</th>";
        echo "<tr bgcolor=\"".$background."\"><td>".$data[0]['avatar_name']."</td><td>".$data[0]['id']."</td></tr>";
        echo "</table><br>";
        echo "<table border=\"0\" cellpadding=\"5\"><th>Region name</th><th>Parcel name</th><th>Datetime</th>";
        $bgrow = -1;
        foreach ($data AS $dat)
        {
            $background = "#F0F0F0";
            if ($bgrow==1) $background = "#C0C0C0";
            $bgrow = $bgrow * -1;
            echo "<tr bgcolor=\"".$background."\"><td>".$dat['region_name']."</td><td>".$dat['parcel_name']."</td><td>".$dat['ddate']." ".$dat['dtime']."</td></tr>";
        }
        echo "</table>";
        
        for ($i=1; $i<$page; $i++) { 
            echo "<a href=\"index.php?task=showAvatar&name=".$dat['id']."&page=".$i."\"> ".$i." </a> "; 
        };
        if (count($data)==$itemsPerPage) echo " <a href=\"index.php?task=showAvatar&name=".$data[0]['id']."&page=".($page+1)."\">next</a> "; 
        
        echo "<br><button onclick=\"window.history.back()\">Go Back</button>";
        echo "</body></html>";         

    }
    
    function showAvatarsOnParcel($data,$page,$itemsPerPage,$extra)
    {
        echo "<html><body><table border=\"0\" cellpadding=\"5\"><th>Region name</th><th>Parcel name</th><th>Owner</th>";
        $group = "";
        if ($dat['parcelGroupOwned']==1) $group = " (group)";
        echo "<tr bgcolor=\"".$background."\"><td>".$data[0]['region_name']."</td><td>".$data[0]['parcel_name']."</td><td>".$data[0]['ParcelOwnerName'].$group."</td></tr>";
        echo "</table><br>";
        echo "<table border=\"0\" cellpadding=\"5\"><th>Avatar name</th><th>Avatar uuid</th><th>Datetime</th>";
        $bgrow = -1;
        foreach ($data AS $dat)
        {
            $background = "#F0F0F0";
            if ($bgrow==1) $background = "#C0C0C0";
            $bgrow = $bgrow * -1;
            echo "<tr bgcolor=\"".$background."\"><td><a href=\"index.php?task=showAvatar&name=".$dat['id']."\">".$dat['avatar_name']."</a></td><td>".$dat['id']."</td><td>".$dat['ddate']." ".$dat['dtime']."</td></tr>";
        }
        echo "</table>";
        
        for ($i=1; $i<$page; $i++) { 
            echo "<a href=\"index.php?task=showAvatarsOnParcel&name=".$data[0]['pid'].$extra."&page=".$i."\"> ".$i." </a> "; 
        };
        if (count($data)==$itemsPerPage) echo " <a href=\"index.php?task=showAvatarsOnParcel&name=".$extra.$dat[0]['pid']."&page=".($page+1)."\">next</a> "; 
        
        echo "<br><button onclick=\"window.history.back()\">Go Back</button>";
        echo "</body></html>";         
    }
    
    function showParcel($data,$page,$itemsPerPage)
    {
        echo "<html><body><table border=\"0\" cellpadding=\"5\"><th>Region name</th><th>Parcel name</th><th>Parcel uuid</th><th>Owner</th><th>date:hour</th><th>total visitors</th>";
        $bgrow = -1;
        foreach ($data AS $dat)
        {
            $background = "#F0F0F0";
            if ($bgrow==1) $background = "#C0C0C0";
            $bgrow = $bgrow * -1;
            $group = "";
            if ($dat['parcelGroupOwned']==1) $group = " (group)";
            echo "<tr bgcolor=\"".$background."\"><td>".$dat['region_name']."</td><td>".$dat['parcel_name']."</td><td>".$dat['pid']."</td><td>".$dat['ParcelOwnerName'].$group."</td><td><a title=\"avatars per day\" href=\"index.php?task=showAvatarsOnParcel&name=".$dat['pid']."&date=".$dat['ddate']."\">".$dat['ddate']."</a>:<a title=\"avatars per hour\" href=\"index.php?task=showAvatarsOnParcel&name=".$dat['pid']."&date=".$dat['ddate']."&hour=".$dat['dhour']."\">".$dat['dhour']."</a></td><td>".$dat['total']."</td></tr>";
        }
        echo "</table>";
        
        for ($i=1; $i<$page; $i++) { 
            echo "<a href=\"index.php?task=showParcel&name=".$data[0]['pid']."&page=".$i."\"> ".$i." </a> "; 
        };
        if (count($data)==$itemsPerPage) echo " <a href=\"index.php?task=showParcel&name=".$data[0]['pid']."&page=".($page+1)."\">next</a> "; 
 
        echo "<br><button onclick=\"window.history.back()\">Go Back</button>";
        echo "</body></html>"; 
    }
}
?>