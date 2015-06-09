// Adapters.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "Adapters.h"

extern "C" {
#include "hidsdi.h"
}

#define MAX_GAMEPADS 32

static TCHAR *deviceNames[MAX_GAMEPADS];
static HANDLE deviceHandles[MAX_GAMEPADS];
static UINT devicesActive;


#define CHECK(exp)		{ if(!(exp)) goto Error; }
#define SAFE_FREE(p)	{ if(p) { HeapFree(hHeap, 0, p); (p) = NULL; } }

static BOOL ReadDeviceName(int index, HANDLE handle)
{
	UINT size = 0;
	UINT result = GetRawInputDeviceInfo(handle,
		RIDI_DEVICENAME, NULL, &size);
	if (result != 0) {
		result = GetLastError();
		return false;
	}

	deviceNames[index] = (TCHAR *)HeapAlloc(GetProcessHeap(), 0, size * sizeof(TCHAR));
	result = GetRawInputDeviceInfo(handle,
		RIDI_DEVICENAME, deviceNames[index], &size);
	if (result < 0) {
		result = GetLastError();
		HeapFree(GetProcessHeap(), 0, deviceNames[index]);
		deviceHandles[index] = NULL;
		deviceNames[index] = NULL;
		return false;
	}

	deviceHandles[index] = handle;
	return TRUE;
}


static BOOL ReadDeviceInfo(int index, HANDLE handle)
{
	// May be a gamepad
	RID_DEVICE_INFO *info;

	UINT size = 0; // sizeof(RID_DEVICE_INFO);
	UINT result = GetRawInputDeviceInfo(handle, RIDI_DEVICEINFO,
		NULL, &size);

	info = (RID_DEVICE_INFO *) HeapAlloc(GetProcessHeap(), 0, size);

	result = GetRawInputDeviceInfo(handle, RIDI_DEVICEINFO,
		info, &size);

	if (result != (UINT)-1) {
		if (info->hid.usUsagePage == 1 && 
		(info->hid.usUsage == 4 || info->hid.usUsage == 5))  {
			// It is a gamepad!!!
						
			HeapFree(GetProcessHeap(), 0, info);
			return ReadDeviceName(index, handle);;
		}
	} else {
		result = (UINT) GetLastError();
	}
	HeapFree(GetProcessHeap(), 0, info);
	return FALSE;
}

static int FindIndex(HANDLE handle)
{
	for(int i = 0; i < MAX_GAMEPADS; i++)
		if (deviceHandles[i] == handle)
			return i;

	return -1;
}

ADAPTERS_API int InitialiseGamepads(HWND handle)
{
	RAWINPUTDEVICE devices[2];

	devices[0].usUsagePage = 1;
	devices[0].usUsage = 4; // Joystick
	devices[0].dwFlags = 0 ;
	devices[0].hwndTarget = handle;
	devices[1].usUsagePage = 1;
	devices[1].usUsage = 5;
	devices[1].dwFlags = 0 ;
	devices[1].hwndTarget = handle;

	bool result = RegisterRawInputDevices(devices, 2, sizeof(RAWINPUTDEVICE));
	if (! result) {
		DWORD error = GetLastError();
		return 0;
	}

	UINT numdevices;
	GetRawInputDeviceList(NULL, &numdevices, sizeof(RAWINPUTDEVICELIST));
	RAWINPUTDEVICELIST *list = (RAWINPUTDEVICELIST *) HeapAlloc(GetProcessHeap(), 0, 
		numdevices * sizeof(RAWINPUTDEVICELIST));
	GetRawInputDeviceList(list, &numdevices, sizeof(RAWINPUTDEVICELIST));

	for(UINT i = 0; i < numdevices; i++) {
		if (list[i].dwType == RIM_TYPEHID) {
			if (ReadDeviceInfo(devicesActive, list[i].hDevice))
				devicesActive++;
		}

	}

	return devicesActive;
}

ADAPTERS_API TCHAR *GetDevicePath(int index)
{
	if (index < 0 || index >= MAX_GAMEPADS)
		return NULL;

	return deviceNames[index];
}


ADAPTERS_API int PollDeviceChange()
{
	// Read device list
	UINT numdevices;
	GetRawInputDeviceList(NULL, &numdevices, sizeof(RAWINPUTDEVICELIST));
	RAWINPUTDEVICELIST *list = (RAWINPUTDEVICELIST *) HeapAlloc(GetProcessHeap(), 0, 
		numdevices * sizeof(RAWINPUTDEVICELIST));
	GetRawInputDeviceList(list, &numdevices, sizeof(RAWINPUTDEVICELIST));

	BOOL found[MAX_GAMEPADS];
	for(int i = 0; i < MAX_GAMEPADS; i++)
		found[i] = FALSE;

	for(UINT i = 0; i < numdevices; i++) {
		if (list[i].dwType == RIM_TYPEHID) {
			int index = FindIndex(list[i].hDevice);

			if (index >= 0)
				found[index] = true;
			else {
				// Find empty slot. Only process one change at a time, to simplify
				// interface to managed code
				for(int j = 0; j < MAX_GAMEPADS; j++)
					if (! deviceNames[j]) {
						if (ReadDeviceInfo(j, list[i].hDevice))
							return j;
					}
			}
		}
	}

	for(int i = 0; i < MAX_GAMEPADS; i++) {
		if (deviceNames[i] && ! found[i]) {
			// Missing device
			deviceHandles[i] = NULL;
			HeapFree(GetProcessHeap(), 0, deviceNames[i]);
			deviceNames[i] = NULL;
			return i;
		}
	}

	return -1;
}

static void ParseRawInput(PRAWINPUT pRawInput,
	unsigned char *buttons, int *x, int *y)
{
	PHIDP_PREPARSED_DATA pPreparsedData;
	HIDP_CAPS            Caps;
	PHIDP_BUTTON_CAPS    pButtonCaps;
	PHIDP_VALUE_CAPS     pValueCaps;
	USHORT               capsLength;
	UINT                 bufferSize;
	HANDLE               hHeap;
	ULONG                i, usageLength, value;

	pPreparsedData = NULL;
	pButtonCaps    = NULL;
	pValueCaps     = NULL;
	PUSAGE usages = NULL;
	hHeap          = GetProcessHeap();

	//
	// Get the preparsed data block
	//

	CHECK( GetRawInputDeviceInfo(pRawInput->header.hDevice, RIDI_PREPARSEDDATA, NULL, &bufferSize) == 0 );
	CHECK( pPreparsedData = (PHIDP_PREPARSED_DATA)HeapAlloc(hHeap, 0, bufferSize) );
	CHECK( (int)GetRawInputDeviceInfo(pRawInput->header.hDevice, RIDI_PREPARSEDDATA, pPreparsedData, &bufferSize) >= 0 );

	//
	// Get the joystick's capabilities
	//

	// Button caps
	CHECK( HidP_GetCaps(pPreparsedData, &Caps) == HIDP_STATUS_SUCCESS )
	CHECK( pButtonCaps = (PHIDP_BUTTON_CAPS)HeapAlloc(hHeap, 0, sizeof(HIDP_BUTTON_CAPS) * Caps.NumberInputButtonCaps) );

	capsLength = Caps.NumberInputButtonCaps;
	CHECK( HidP_GetButtonCaps(HidP_Input, pButtonCaps, &capsLength, pPreparsedData) == HIDP_STATUS_SUCCESS )
	int g_NumberOfButtons = pButtonCaps->Range.UsageMax - pButtonCaps->Range.UsageMin + 1;

	// Value caps
	CHECK( pValueCaps = (PHIDP_VALUE_CAPS)HeapAlloc(hHeap, 0, sizeof(HIDP_VALUE_CAPS) * Caps.NumberInputValueCaps) );
	capsLength = Caps.NumberInputValueCaps;
	CHECK( HidP_GetValueCaps(HidP_Input, pValueCaps, &capsLength, pPreparsedData) == HIDP_STATUS_SUCCESS )

	//
	// Get the pressed buttons - only length required to tell if any are pressed
	//

	usageLength = HidP_MaxUsageListLength(HidP_Input, pButtonCaps->UsagePage, pPreparsedData);
	usages = (PUSAGE)HeapAlloc(hHeap, 0, usageLength * sizeof(USAGE));
	CHECK(
		HidP_GetUsages(
			HidP_Input, pButtonCaps->UsagePage, 0, usages, &usageLength, pPreparsedData,
			(PCHAR)pRawInput->data.hid.bRawData, pRawInput->data.hid.dwSizeHid
		) == HIDP_STATUS_SUCCESS );

	*buttons = (usageLength > 0);

	//
	// Get the state of discrete-valued-controls
	//

	for(i = 0; i < Caps.NumberInputValueCaps; i++)
	{
		CHECK(
			HidP_GetUsageValue(
				HidP_Input, pValueCaps[i].UsagePage, 0, pValueCaps[i].Range.UsageMin, &value, pPreparsedData,
				(PCHAR)pRawInput->data.hid.bRawData, pRawInput->data.hid.dwSizeHid
			) == HIDP_STATUS_SUCCESS );

		switch(pValueCaps[i].Range.UsageMin)
		{
		case 0x30:	// X-axis
			*x = (LONG)value - 128;
			break;

		case 0x31:	// Y-axis
			*y = (LONG)value - 128;
			break;

		}
	}

	//
	// Clean up
	//

Error:
	SAFE_FREE(pPreparsedData);
	SAFE_FREE(pButtonCaps);
	SAFE_FREE(pValueCaps);
	SAFE_FREE(usages);
}

ADAPTERS_API int ProcessInput(HANDLE wParam, HANDLE lParam, unsigned char *buttons, int *x, int *y)
{
	UINT      bufferSize;

	GetRawInputData((HRAWINPUT)lParam, RID_INPUT, NULL, &bufferSize, sizeof(RAWINPUTHEADER));

	PRAWINPUT pRawInput = (PRAWINPUT)HeapAlloc(GetProcessHeap(), 0, bufferSize);
	if(!pRawInput)
		return -1;

	GetRawInputData((HRAWINPUT)lParam, RID_INPUT, pRawInput, &bufferSize, sizeof(RAWINPUTHEADER));
	int index = FindIndex(pRawInput->header.hDevice);

	if (index < 0) {
		for(int j = 0; j < MAX_GAMEPADS; j++)
			if (! deviceNames[j]) {
				if (ReadDeviceName(j, pRawInput->header.hDevice)) {
					index = j;
					break;
				}
			}
	}
		
	ParseRawInput(pRawInput, buttons, x, y);

	HeapFree(GetProcessHeap(), 0, pRawInput);


	return index;
}