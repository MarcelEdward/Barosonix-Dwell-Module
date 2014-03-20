<?PHP
include("databaseinfo.php");

mysql_connect ($DB_HOST, $DB_USER, $DB_PASSWORD);
mysql_select_db ($DB_NAME);

$report ="";
$xmlrpc_server = xmlrpc_server_create();


xmlrpc_server_register_method($xmlrpc_server, "GetDwell",
		"GetDwell");

function GetDwell($method_name, $params, $app_data)
{
	 $req            = $params[0];
 
    $pid           = $req['pid'];
	$dwell = "0";
 
 
   $result = mysql_query("SELECT * FROM land WHERE ".
            "UUID = '".mysql_escape_string($pid) ."'");

    $data = array();
    while (($row = mysql_fetch_assoc($result)))
    {
	$dwell = $row["Dwell"];
       
    }

	 $data[] = array(
			"dwellers" => $dwell);
    $response_xml = xmlrpc_encode(array(
        'success'      => True,
        'errorMessage' => "",
        'data' => $data
    ));

    print $response_xml;
}

xmlrpc_server_register_method($xmlrpc_server, "Checkav",
		"Checkav");

function Checkav($method_name, $params, $app_data)
{
	$req 			= $params[0];

	$id			= $req['id'];
	$pid			= $req['pid'];
	$savrt			= $req['avrt'];

	
	$multiplyer = 60;
	$avrt = $savrt * $multiplyer;


	$timestamp		= time();

	

	 $result = mysql_query("SELECT * FROM landDwell WHERE ".
            "id = '". mysql_escape_string($id) ."' AND ".
            "pid = '". mysql_escape_string($pid) ."'");
 
    $row = mysql_fetch_assoc($result);
if ($row != False)
{
$otimestamp = $row["timestamp"];
$ctimestamp = $timestamp;
$dif = ($ctimestamp-$otimestamp);
if($dif > $avrt) 
{
$report = "bigger than 300"; 
$query1 = "UPDATE landDwell SET timestamp = '".$timestamp."' WHERE id = '".$id."' AND pid = '".$pid."'";
$result1 = mysql_query($query1);
$result2 = mysql_query("SELECT * FROM land WHERE ".
            "UUID = '". mysql_escape_string($pid) ."'");
$row2 = mysql_fetch_assoc($result2);
$dwell = $row2["Dwell"];
$dwell = $dwell +1;
$query3 = "UPDATE land SET Dwell = '".$dwell."' WHERE UUID = '".$pid."'";
$result3 = mysql_query($query3);
}
}
else
{
$query = ("INSERT INTO landDwell (id, pid, timestamp) VALUES ('$id', '$pid','$timestamp')");
$result = mysql_query($query);
$result2 = mysql_query("SELECT * FROM land WHERE ".
            "UUID = '". mysql_escape_string($pid) ."'");
$row2 = mysql_fetch_assoc($result2);
$dwell = $row2["Dwell"];
$dwell = $dwell +1;
$query3 = "UPDATE land SET Dwell = '".$dwell."' WHERE UUID = '".$pid."'";
$result3 = mysql_query($query3);
}
	
	$response_xml = xmlrpc_encode(array(
		'success'	  => True,
		'errorMessage' => ""
	));

	print $response_xml;
}


$request_xml = $HTTP_RAW_POST_DATA;
xmlrpc_server_call_method($xmlrpc_server, $request_xml, '');
xmlrpc_server_destroy($xmlrpc_server);
?>


