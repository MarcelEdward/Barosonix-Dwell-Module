	using System;
	using System.Reflection;
	using Mono.Addins;
	using Nini.Config;
	using OpenSim.Framework;
	using OpenSim.Region.Framework;
	using OpenSim.Region.Framework.Interfaces;
	using OpenSim.Region.Framework.Scenes;
	using OpenMetaverse;
	using OpenSim.Services.Interfaces;
	using System.Text;
	using OpenMetaverse.Assets;
	using System.Collections;
	using OpenSim.Region.Framework.Scenes.Serialization;
	using System.Collections.Generic;
	using PermissionMask = OpenSim.Framework.PermissionMask;

	[assembly: Addin("MyRegionModule", "0.1")]
	[assembly: AddinDependency("OpenSim", "0.5")]

	namespace ModSendCommandExample
	{
		[Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule")]
		public class MyRegionModule : INonSharedRegionModule
		{
			private IScriptModuleComms m_commsMod = null;
			private IInventoryService inventoryService;
			private List<Scene> m_Scenes = new List<Scene> ();

			
			public string Name 
			{
				get {
					return "MyRegionModule";
				}
			}
			
			public Type ReplaceableInterface 
			{
				get {
					return null;
				}
			}
			
			public void Initialise(IConfigSource source) {}
			
			public void Close() {}
			
			public void AddRegion(Scene scene) 
			{
				lock (m_Scenes)
				{
					m_Scenes.Add(scene);
				}
			}
			
			public void RemoveRegion(Scene scene) {
				lock (m_Scenes)
				{
					m_Scenes.Remove(scene);
				}
			}

			public void RegionLoaded(Scene scene) 
			{
				m_commsMod = scene.RequestModuleInterface<IScriptModuleComms>();
				m_commsMod.OnScriptCommand += ProcessScriptCommand;		      		
			}

			private IClientAPI LocateClient (UUID agentID)
			{
				foreach (Scene current in m_Scenes) {
					ScenePresence scenePresence = current.GetScenePresence (agentID);
					if (scenePresence != null && !scenePresence.IsChildAgent) {
						return scenePresence.ControllingClient;
					}
				}
				return null;
			}

			public AssetBase CreateAsset(string name, string description, sbyte assetType, byte[] data, UUID creatorID)
			{
				AssetBase asset = new AssetBase(UUID.Random(), name, assetType, creatorID.ToString());
				asset.Description = description;
				asset.Data = (data == null) ? new byte[1] : data;
				
				return asset;
			}
			void ProcessScriptCommand(UUID scriptId, string reqId, string module, string input, string k)
			{
				if ("MYMOD" != module)
					return;
				
				string[] tokens = input.Split(new char[] { '|' }, StringSplitOptions.None);
				
				string command = tokens[0];
				switch (command)
				{
				case "Greet":
					string name = tokens[1];
					m_commsMod.DispatchReply(scriptId, 1, "Hello " + name, "");
					break;
				case "test":

					UUID newFolderID = UUID.Random();
					UUID assID = UUID.Random();
					string uid = tokens[1];
					UUID user = (UUID)uid;
					IClientAPI client = LocateClient(user);
					Scene scene = (Scene)client.Scene;
					inventoryService = scene.InventoryService;
					InventoryFolderBase rootFolder = inventoryService.GetRootFolder (user);
					InventoryFolderBase newFolder = new InventoryFolderBase(newFolderID, "M24", user, -1, rootFolder.ID, rootFolder.Version);
					inventoryService.AddFolder(newFolder);
					test (scene,assID,"m23",user ,newFolderID);
					//m_commsMod.DispatchReply(scriptId, 1, "Hello " + name, "");
					ScenePresence avatar = null;
					if (scene.TryGetScenePresence(client.AgentId, out avatar))
					{ 
						scene.SendInventoryUpdate(avatar.ControllingClient, rootFolder, true, false);
					}
					break;
				}
			}

		public static AssetBase CreateAsset(UUID assetUuid, AssetType assetType, byte[] data, UUID creatorID)
		{
			AssetBase asset = new AssetBase(assetUuid, assetUuid.ToString(), (sbyte)assetType, creatorID.ToString());
			asset.Data = data;
			return asset;
		}



			public void test(Scene scene ,UUID assetID ,String assetName ,UUID AgentID ,UUID folder)
			{ 
			sbyte assType = 0;
				sbyte inType = 0;
					inType = (sbyte)InventoryType.Object;
					assType = (sbyte)AssetType.Object;
					
			SceneObjectGroup so = SceneObjectSerializer.FromOriginalXmlFormat(xml);
			SceneObjectPart part = so.RootPart;

			AssetScriptText ast = new AssetScriptText();
			ast.Source = "default { state_entry() { llSay(0, \"Hello World\"); } }";
			ast.Encode();
			
			UUID assetUuid = UUID.Random();
			UUID itemUuid = UUID.Random();
			
			AssetBase asset1
				= CreateAsset(assetUuid, AssetType.LSLText, ast.AssetData, UUID.Zero);
			scene.AssetService.Store(asset1);
			TaskInventoryItem item
				= new TaskInventoryItem 
			{ Name = "blerg", AssetID = assetUuid, ItemID = itemUuid,
				Type = (int)AssetType.LSLText, InvType = (int)InventoryType.LSL };
			part.Inventory.AddInventoryItem(item, true);

			byte[] data1 = ASCIIEncoding.ASCII.GetBytes(SceneObjectSerializer.ToOriginalXmlFormat(so));

					
					
				AssetBase asset;
				asset = new AssetBase(assetID, assetName, assType, AgentID.ToString());
				asset.Data = data1;
				scene.AssetService.Store(asset);



				InventoryItemBase item1 = new InventoryItemBase();
				item1.Owner = AgentID;
				item1.CreatorId = AgentID.ToString();
				item1.CreatorData = String.Empty;
				item1.ID = UUID.Random();
				item1.AssetID = asset.FullID;
				item1.Description = "test";
				item1.Name = assetName;
				item1.AssetType = assType;
				item1.InvType = inType;
				item1.Folder = folder;
				
				// If we set PermissionMask.All then when we rez the item the next permissions will replace the current
				// (owner) permissions.  This becomes a problem if next permissions are changed.
				item1.CurrentPermissions
					= (uint)(PermissionMask.Move);
				
			item1.BasePermissions = (uint)(PermissionMask.Move);
				item1.EveryOnePermissions = 0;
			item1.NextPermissions = (uint)(PermissionMask.Move);
				item1.CreationDate = Util.UnixTimeSinceEpoch();
				scene.InventoryService.AddItem(item1);



			}

		private string xml = @"
        <SceneObjectGroup>
            <RootPart>
                <SceneObjectPart xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
                    <AllowedDrop>false</AllowedDrop>
                    <CreatorID><Guid>a6dacf01-4636-4bb9-8a97-30609438af9d</Guid></CreatorID>
                    <FolderID><Guid>e6a5a05e-e8cc-4816-8701-04165e335790</Guid></FolderID>
                    <InventorySerial>1</InventorySerial>
                    <TaskInventory />
                    <ObjectFlags>0</ObjectFlags>
                    <UUID><Guid>e6a5a05e-e8cc-4816-8701-04165e335790</Guid></UUID>
                    <LocalId>2698615125</LocalId>
                    <Name>PrimMyRide</Name>
                    <Material>0</Material>
                    <PassTouches>false</PassTouches>
                    <RegionHandle>1099511628032000</RegionHandle>
                    <ScriptAccessPin>0</ScriptAccessPin>
                    <GroupPosition><X>147.23</X><Y>92.698</Y><Z>22.78084</Z></GroupPosition>
<OffsetPosition><X>0</X><Y>0</Y><Z>0</Z></OffsetPosition>
                    <RotationOffset><X>-4.371139E-08</X><Y>-1</Y><Z>-4.371139E-08</Z><W>0</W></RotationOffset>
                    <Velocity><X>0</X><Y>0</Y><Z>0</Z></Velocity>
                    <RotationalVelocity><X>0</X><Y>0</Y><Z>0</Z></RotationalVelocity>
                    <AngularVelocity><X>0</X><Y>0</Y><Z>0</Z></AngularVelocity>
                    <Acceleration><X>0</X><Y>0</Y><Z>0</Z></Acceleration>
                    <Description />
                    <Color />
                    <Text />
                    <SitName />
                    <TouchName />
                    <LinkNum>0</LinkNum>
                    <ClickAction>0</ClickAction>
                    <Shape>
                        <ProfileCurve>1</ProfileCurve>
                        <TextureEntry>AAAAAAAAERGZmQAAAAAABQCVlZUAAAAAQEAAAABAQAAAAAAAAAAAAAAAAAAAAA==</TextureEntry>
                        <ExtraParams>AA==</ExtraParams>
                        <PathBegin>0</PathBegin>
                        <PathCurve>16</PathCurve>
                        <PathEnd>0</PathEnd>
                        <PathRadiusOffset>0</PathRadiusOffset>
                        <PathRevolutions>0</PathRevolutions>
                        <PathScaleX>100</PathScaleX>
                        <PathScaleY>100</PathScaleY>
                        <PathShearX>0</PathShearX>
                        <PathShearY>0</PathShearY>
                        <PathSkew>0</PathSkew>
                        <PathTaperX>0</PathTaperX>
                        <PathTaperY>0</PathTaperY>
                        <PathTwist>0</PathTwist>
                        <PathTwistBegin>0</PathTwistBegin>
                        <PCode>9</PCode>
                        <ProfileBegin>0</ProfileBegin>
                        <ProfileEnd>0</ProfileEnd>
                        <ProfileHollow>0</ProfileHollow>
                        <Scale><X>10</X><Y>10</Y><Z>0.5</Z></Scale>
                        <State>0</State>
                        <ProfileShape>Square</ProfileShape>
                        <HollowShape>Same</HollowShape>
                        <SculptTexture><Guid>00000000-0000-0000-0000-000000000000</Guid></SculptTexture>
                        <SculptType>0</SculptType><SculptData />
                        <FlexiSoftness>0</FlexiSoftness>
                        <FlexiTension>0</FlexiTension>
                        <FlexiDrag>0</FlexiDrag>
                        <FlexiGravity>0</FlexiGravity>
                        <FlexiWind>0</FlexiWind>
                        <FlexiForceX>0</FlexiForceX>
                        <FlexiForceY>0</FlexiForceY>
                        <FlexiForceZ>0</FlexiForceZ>
                        <LightColorR>0</LightColorR>
                        <LightColorG>0</LightColorG>
                        <LightColorB>0</LightColorB>
                        <LightColorA>1</LightColorA>
                        <LightRadius>0</LightRadius>
                        <LightCutoff>0</LightCutoff>
                        <LightFalloff>0</LightFalloff>
                        <LightIntensity>1</LightIntensity>
                        <FlexiEntry>false</FlexiEntry>
                        <LightEntry>false</LightEntry>
                        <SculptEntry>false</SculptEntry>
                    </Shape>
                    <Scale><X>10</X><Y>10</Y><Z>0.5</Z></Scale>
                    <UpdateFlag>0</UpdateFlag>
                    <SitTargetOrientation><X>0</X><Y>0</Y><Z>0</Z><W>1</W></SitTargetOrientation>
                    <SitTargetPosition><X>0</X><Y>0</Y><Z>0</Z></SitTargetPosition>
                    <SitTargetPositionLL><X>0</X><Y>0</Y><Z>0</Z></SitTargetPositionLL>
                    <SitTargetOrientationLL><X>0</X><Y>0</Y><Z>0</Z><W>1</W></SitTargetOrientationLL>
                    <ParentID>0</ParentID>
                    <CreationDate>1211330445</CreationDate>
                    <Category>0</Category>
                    <SalePrice>0</SalePrice>
                    <ObjectSaleType>0</ObjectSaleType>
                    <OwnershipCost>0</OwnershipCost>
                    <GroupID><Guid>00000000-0000-0000-0000-000000000000</Guid></GroupID>
                    <OwnerID><Guid>a6dacf01-4636-4bb9-8a97-30609438af9d</Guid></OwnerID>
                    <LastOwnerID><Guid>a6dacf01-4636-4bb9-8a97-30609438af9d</Guid></LastOwnerID>
                    <BaseMask>2147483647</BaseMask>
                    <OwnerMask>2147483647</OwnerMask>
                    <GroupMask>0</GroupMask>
                    <EveryoneMask>0</EveryoneMask>
                    <NextOwnerMask>2147483647</NextOwnerMask>
                    <Flags>None</Flags>
                    <CollisionSound><Guid>00000000-0000-0000-0000-000000000000</Guid></CollisionSound>
                    <CollisionSoundVolume>0</CollisionSoundVolume>
                    <DynAttrs>
                        <llsd>
                            <map>
                                <key>MyNamespace</key>
                                <map>                                
                                    <key>MyStore</key>
                                    <map>   
                                        <key>the answer</key>
                                        <integer>42</integer>
                                    </map>
                                </map>
                            </map>
                        </llsd>
                    </DynAttrs>
                </SceneObjectPart>
            </RootPart>
            <OtherParts />
        </SceneObjectGroup>";
			}


		}
