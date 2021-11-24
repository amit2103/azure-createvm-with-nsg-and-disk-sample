using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Samples.Common;
using Microsoft.Azure.Management.Network.Fluent.Models;
using System;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace ManageVirtualMachine
{
    public class Program
    {
        /**
         * Azure Compute sample for managing virtual machines -
         *  - Create a virtual machine with managed OS Disk
         *  - Start a virtual machine
         *  - Stop a virtual machine
         *  - Restart a virtual machine
         *  - Update a virtual machine
         *    - Tag a virtual machine (there are many possible variations here)
         *    - Attach data disks
         *    - Detach data disks
         *  - List virtual machines
         *  - Delete a virtual machine.
         */
        public static void RunSample(IAzure azure)
        {
            string frontEndNSGName = SdkContext.RandomResourceName("fensg", 24);
            string backEndNSGName = SdkContext.RandomResourceName("bensg", 24);
            string networkInterfaceName1 = SdkContext.RandomResourceName("nic1", 24);
            string vnetName = SdkContext.RandomResourceName("vnet", 24);
            var region = Region.USEast;
            var windowsVmName = Utilities.CreateRandomName("wVM");
            var linuxVmName = Utilities.CreateRandomName("lVM");
            var rgName = "testRGViaApi";
            var userName = Utilities.CreateUsername();
            var password = Utilities.CreatePassword();
            string publicIPAddressLeafDNS1 = SdkContext.RandomResourceName("pip1", 24);

            try
            {
                Utilities.Log("Creating a virtual network ...");

                var network = azure.Networks.Define(vnetName)
                        .WithRegion(Region.USEast)
                        .WithNewResourceGroup(rgName)
                        .WithAddressSpace("172.16.0.0/16")
                        .DefineSubnet("Front-end")
                            .WithAddressPrefix("172.16.1.0/24")
                            .Attach()
                        .DefineSubnet("Back-end")
                            .WithAddressPrefix("172.16.2.0/24")
                            .Attach()
                        .Create();

                Utilities.Log("Created a virtual network: " + network.Id);
                Utilities.PrintVirtualNetwork(network);

                Utilities.Log("Creating a security group for the front end - allows RDP and HTTP");
                var frontEndNSG = azure.NetworkSecurityGroups.Define(frontEndNSGName)
                        .WithRegion(Region.USEast)
                        .WithExistingResourceGroup(rgName)
                        .DefineRule("ALLOW-RDP")
                            .AllowInbound()
                            .FromAnyAddress()
                            .FromAnyPort()
                            .ToAnyAddress()
                            .ToPort(3389)
                            .WithProtocol(SecurityRuleProtocol.Tcp)
                            .WithPriority(100)
                            .WithDescription("Allow RDP")
                            .Attach()
                        .DefineRule("ALLOW-HTTP")
                            .AllowInbound()
                            .FromAnyAddress()
                            .FromAnyPort()
                            .ToAnyAddress()
                            .ToPort(80)
                            .WithProtocol(SecurityRuleProtocol.Tcp)
                            .WithPriority(101)
                            .WithDescription("Allow HTTP")
                            .Attach()
                        .Create();

                Utilities.Log("Created a network security group for the front end - allows RDP and HTTP, ID is: "  + frontEndNSG.Id);


                //=============================================================
                // Create a Windows virtual machine

                // Prepare a creatable data disk for VM
                //
                var dataDiskCreatable = azure.Disks.Define(Utilities.CreateRandomName("dsk-"))
                        .WithRegion(region)
                        .WithExistingResourceGroup(rgName)
                        .WithData()
                        .WithSizeInGB(100);

                // Create a data disk to attach to VM
                //
                var dataDisk = azure.Disks.Define(Utilities.CreateRandomName("dsk-"))
                        .WithRegion(region)
                        .WithExistingResourceGroup(rgName)
                        .WithData()
                        .WithSizeInGB(50)
                        .Create();


                Utilities.Log("Creating a network interface for the front end");

                var networkInterface = azure.NetworkInterfaces.Define(networkInterfaceName1)
                        .WithRegion(Region.USEast)
                        .WithExistingResourceGroup(rgName)
                        .WithExistingPrimaryNetwork(network)
                        .WithSubnet("Front-end")
                        .WithPrimaryPrivateIPAddressDynamic()
                        .WithNewPrimaryPublicIPAddress(publicIPAddressLeafDNS1)
                        .WithIPForwarding()
                        .WithExistingNetworkSecurityGroup(frontEndNSG)
                        .Create();

                Utilities.Log("Created network interface for the front end with ID : " + networkInterface.Id);

                Utilities.Log("Creating a Windows VM");

                var t1 = new DateTime();

                var windowsVM = azure.VirtualMachines.Define(windowsVmName)
                        .WithRegion(region)
                        .WithExistingResourceGroup(rgName)
                        .WithExistingPrimaryNetworkInterface(networkInterface)
                        .WithPopularWindowsImage(KnownWindowsVirtualMachineImage.WindowsServer2012R2Datacenter)
                        .WithAdminUsername(userName)
                        .WithAdminPassword(password)
                        .WithNewDataDisk(10)
                        .WithNewDataDisk(dataDiskCreatable)
                        .WithExistingDataDisk(dataDisk)
                        .WithSize(VirtualMachineSizeTypes.Parse("Standard_DS2_v2"))
                        .Create();

                var t2 = new DateTime();
                Utilities.Log($"Created VM: (took {(t2 - t1).TotalSeconds} seconds) " + windowsVM.Id);
                var publicIPAddress = windowsVM.GetPrimaryPublicIPAddress();
                Utilities.Log($"IP of VM  for mstsc: (took {publicIPAddress.IPAddress} ) ");
                // Print virtual machine details
                Utilities.PrintVirtualMachine(windowsVM);

                //=============================================================
                // Update - Tag the virtual machine

                windowsVM.Update()
                        .WithTag("who-rocks", "java")
                        .WithTag("where", "on azure")
                        .Apply();

                Utilities.Log("Tagged VM: " + windowsVM.Id);

                //=============================================================
                // Update - Add data disk

                windowsVM.Update()
                        .WithNewDataDisk(10)
                        .Apply();

                Utilities.Log("Added a data disk to VM" + windowsVM.Id);
                Utilities.PrintVirtualMachine(windowsVM);

                Console.Write($"Attached data disk you can check via mstsc if needed ,ip :  {publicIPAddress.IPAddress}  After you are done Press Any key to continue: ");
                Console.ReadLine();

                //=============================================================
                // Update - detach data disk

                windowsVM.Update()
                        .WithoutDataDisk(0)
                        .Apply();

                Utilities.Log("Detached data disk at lun 0 from VM " + windowsVM.Id);

                Utilities.Log("Removed attached data disk from VM,  you can check via mstsc if needed , Press Any key to continue: ");
                Console.ReadLine();

                //=============================================================
                // Restart the virtual machine

                Utilities.Log("Restarting VM: " + windowsVM.Id);

                windowsVM.Restart();

                Utilities.Log("Restarted VM: " + windowsVM.Id + "; state = " + windowsVM.PowerState);

                //=============================================================
                // Stop (powerOff) the virtual machine

                Utilities.Log("Powering OFF VM: " + windowsVM.Id);

                windowsVM.PowerOff();

                Utilities.Log("Powered OFF VM: " + windowsVM.Id + "; state = " + windowsVM.PowerState);



                // List virtual machines in the resource group

                var resourceGroupName = windowsVM.ResourceGroupName;

                Utilities.Log("Printing list of VMs =======");

                foreach (var virtualMachine in azure.VirtualMachines.ListByResourceGroup(resourceGroupName))
                {
                    Utilities.PrintVirtualMachine(virtualMachine);
                }

                //=============================================================
                // Delete the virtual machine
                Utilities.Log("Deleting VM: " + windowsVM.Id);

                azure.VirtualMachines.DeleteById(windowsVM.Id);

                Utilities.Log("Deleted VM: " + windowsVM.Id);
            } catch(Exception e)
            {
                Utilities.Log("Error while creating thing: " + rgName);
                Utilities.Log(e);
            }
            finally
            {
                try
                {
                    Utilities.Log("Deleting Resource Group: " + rgName);
                    azure.ResourceGroups.DeleteByName(rgName);
                    Utilities.Log("Deleted Resource Group: " + rgName);
                }
                catch (NullReferenceException)
                {
                    Utilities.Log("Did not create any resources in Azure. No clean up is necessary");
                }
                catch (Exception g)
                {
                    Utilities.Log(g);
                }
            }
        }

        public static void Main(string[] args)
        {
            try
            {
                // Authenticate , expected that AZURE_AUTH_LOCATION ENV variable is set with the full path to the config file
                var credentials = SdkContext.AzureCredentialsFactory.FromFile(Environment.GetEnvironmentVariable("AZURE_AUTH_LOCATION"));

                var azure = Azure.Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(credentials)
                    .WithDefaultSubscription();

                Utilities.Log("Selected subscription: " + azure.SubscriptionId);

                RunSample(azure);
            }
            catch (Exception ex)
            {
                Utilities.Log(ex);
            }
        }
    }
}
