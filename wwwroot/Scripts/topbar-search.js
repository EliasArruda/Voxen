let root, outside, keyboard;
export function initialize(element, reference) {
    dispose();root=element;
    outside=event=>{if(!root.contains(event.target))void reference.invokeMethodAsync('Dismiss').catch(()=>{});};
    keyboard=event=>{
        if(event.target.id!=='topbar-search' || event.isComposing || event.repeat)return;
        if(['ArrowDown','ArrowUp','Enter','Escape'].includes(event.key)){
            event.preventDefault();event.stopPropagation();void reference.invokeMethodAsync('SearchKey',event.key).catch(()=>{});
        }
    };
    document.addEventListener('pointerdown',outside);root.addEventListener('keydown',keyboard);
}
export function dispose(){if(outside)document.removeEventListener('pointerdown',outside);if(root&&keyboard)root.removeEventListener('keydown',keyboard);root=outside=keyboard=undefined;}
