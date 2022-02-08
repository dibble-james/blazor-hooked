﻿// has to be aligned with HxPopover.js!
export function createOrUpdate(element, hxDotnetObjectReference, options) {
	if (!element) {
		return;
	}
	element.hxDotnetObjectReference = hxDotnetObjectReference;
	element.addEventListener('shown.bs.tooltip', handleShown);
	element.addEventListener('hidden.bs.tooltip', handleHidden);
	new bootstrap.Tooltip(element, options);
}

export function show(element) {
	var i = bootstrap.Tooltip.getInstance(element);
	if (i) {
		i.show();
	}
}

export function hide(element) {
	var i = bootstrap.Tooltip.getInstance(element);
	if (i) {
		i.hide();
	}
}

function handleShown(event) {
	event.target.hxDotnetObjectReference.invokeMethodAsync('HxHandleJsShown');
};

function handleHidden(event) {
	event.target.hxDotnetObjectReference.invokeMethodAsync('HxHandleJsHidden');
};

export function destroy(element) {
	if (!element) {
		return;
	}
	element.removeEventListener('shown.bs.tooltip', handleShown);
	element.removeEventListener('hidden.bs.tooltip', handleHidden);
	element.hxDotnetObjectReference = null;
	var tooltip = bootstrap.Tooltip.getInstance(element);
	if (tooltip) {
		tooltip.dispose();
	}
}