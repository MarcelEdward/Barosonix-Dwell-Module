<?PHP
include("databaseinfo.php");

mysql_connect ($DB_HOST, $DB_USER, $DB_PASSWORD);
mysql_select_db ($DB_NAME);

include (__DIR__.'/../xmlrpc/xmlrpc.inc');
include (__DIR__.'/../xmlrpc/xmlrpcs.inc');
include (__DIR__.'/../xmlrpc/xmlrpc_extension_api.inc');
$report ="";
$xmlrpc_server = xmlrpc_server_create();

xmlrpc_server_register_method($xmlrpc_server, "Checkav",
		"Checkav");

function Checkav($method_name, $params, $app_data)
{
	 $req 			= $params[0];
	 //error_log(print_r($req,true));

	 $savrt			= $req['avrt'];
	 $pch			= $req['pch'];
	 $cua 			= $req['cua'];
	 $regionUUID		= $req['regionuuid'];
	 $regionName		= $req['regionname'];
	 $number		= $req['number'];
	
	 $succes = false;
	 for ($i = 0; $i<$number; $i++)
	 {
		  $localLandID		= $req[$i]['localLandID'];
		  $id			= $req[$i]['id'];
		  $pid			= $req[$i]['pid'];	
		  $avatarName		= $req[$i]['avatar'];
		  $parcelName		= $req[$i]['parcel'];
		  $parcelOwnerName	= $req[$i]['parcelOwnerName'];
		  $parcelGroupOwned	= $req[$i]['parcelGroupOwned'];		  
		  $parcelOwner		= $req[$i]['parcelOwner'];
		  
		  $multiplyer = 60;
		  $avrt = $savrt * $multiplyer;
	 
		  $timestamp		= time();
		  
		  $query = "SELECT * FROM landDwell WHERE ".
		     "id = '". mysql_escape_string($id) ."' AND ".
		     "pid = '". mysql_escape_string($pid) ."' ORDER BY timestamp DESC";
		  $result = mysql_query($query);
	  
		  $row = mysql_fetch_assoc($result);
	 
		  if ($row != false)
		  {
			   $otimestamp = $row["timestamp"];
			   $ctimestamp = $timestamp;
			   $dif = ($ctimestamp-$otimestamp);
			   // check if the avatar is on the parcel longer then set time, if so update or insert
			   if($dif > $avrt) 
			   {
				    // if is set to unique avatar count and not longer that the measurement time, then update the record. Otherwise insert a new one
				    if ( $cua == "True" && $dif<($pch*60*60) )
					     $query = "UPDATE landDwell SET timestamp = now() WHERE idkey = ".$row["idkey"];
				    else
					     $query = "INSERT INTO landDwell (id, pid, timestamp, avatar_name, parcel_name, region_uuid, region_name, localLandID, parcelOwner, parcelGroupOwned, parcelOwnerName) VALUES ('$id', '$pid',now(), '$avatarName', '$parcelName', '$regionUUID', '$regionName', '$localLandID', '$parcelOwner', '$parcelGroupOwned', '$parcelOwnerName')";
				    $result = mysql_query($query);
				    $succes = true;
			   }
		  }
		  else
		  {
			   $query = "INSERT INTO landDwell (id, pid, timestamp, avatar_name, parcel_name, region_uuid, region_name, localLandID, parcelOwner, parcelGroupOwned, parcelOwnerName) VALUES ('$id', '$pid',now(), '$avatarName', '$parcelName', '$regionUUID', '$regionName', '$localLandID', '$parcelOwner', '$parcelGroupOwned', '$parcelOwnerName')";
			   $result = mysql_query($query);
			   $succes = true;
		  }	 
	 }

	 if ($succes) {
		  $timestamp = $timestamp - $pch*60*60;
		  // create pid list
		  $pidList = array();
		  for ($i = 0; $i<$number; $i++) $pidList[] = "'".$req[$i]['pid']."'";
		  $pidString = implode(",",$pidList);
		  $query = "SELECT count(*) AS total,localLandID FROM landDwell WHERE pid IN (".$pidString.") AND timestamp >= ".$timestamp;
		  $result = mysql_query($query);
		  $data = array();
		  while ($row = mysql_fetch_assoc($result))
		  {
			$data[] = array("data" => $row["total"], "localLandID" => $row["localLandID"]);
		  }
		  
		  
		  $response_xml = xmlrpc_encode(array(
			   'success'	  => True,
			   'errorMessage' => "",
			   'data' => $data
		  ));
	 } else {
	 	$response_xml = xmlrpc_encode(array(
		'success'	  => False,
		'errorMessage' => ""
		  ));
	 }

	 print $response_xml;
}

$request_xml = $HTTP_RAW_POST_DATA;
xmlrpc_server_call_method($xmlrpc_server, $request_xml, '');
xmlrpc_server_destroy($xmlrpc_server);
?>


