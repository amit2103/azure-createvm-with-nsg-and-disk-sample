# azure-createvm-with-nsg-and-disk-sample
Create a Azure Windows VM with NSG configured for RDP access and attach and remove disks to it


# This sample demonstrates how to manage your Azure virtual machines using a .NET client #

 Azure Compute sample for managing virtual machines -
  - Create a Azure Windows Virtual machine with NSG configured for RDP access and managed disks.
  - Start a virtual machine
  - Stop a virtual machine
  - Restart a virtual machine
  - Update a virtual machine
    - Tag a virtual machine (there are many possible variations here)
    - Attach data disks
    - Detach data disks
  - List virtual machines
  - Delete a virtual machine.
  - Delete the resource group.

To run this sample:

We need to set the environment variable `AZURE_AUTH_LOCATION` with the full path for an auth file. See [how to create an auth file](https://github.com/Azure/azure-libraries-for-net/blob/master/AUTH.md).  There are certain points where a key press is needed to proceed this allows us to check the configuration in portals and RDP into the Windows instance if needed. Simply press any key to proceed.
