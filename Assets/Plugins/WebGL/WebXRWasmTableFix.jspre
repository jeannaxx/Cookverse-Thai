// Cookverse Thai: แก้บั๊ก WebXR Export 0.25.0 กับ Unity 6.5
// webxr.jspre เรียก getWasmTableEntry() แต่ Emscripten ของ Unity 6.5 ตัดฟังก์ชันนี้ทิ้งตอน Build
// (ReferenceError: Can't find variable: getWasmTableEntry ที่ _InitXRSharedArray)
// ไฟล์นี้สร้างฟังก์ชันกลับมาจาก wasmTable ที่ยังอยู่ ถ้า Unity มีของตัวเองอยู่แล้วจะใช้ของเดิม
var getWasmTableEntry = getWasmTableEntry || function (funcPtr) {
  var table = (typeof wasmTable !== 'undefined' && wasmTable) ||
              (typeof wasmExports !== 'undefined' && wasmExports && wasmExports['__indirect_function_table']);
  return table.get(funcPtr);
};
