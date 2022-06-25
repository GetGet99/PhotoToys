function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}
let s = document.createElement('script');
s.src = 'https://cdn.jsdelivr.net/pyodide/v0.20.0/full/pyodide.js';
document.head.append(s);
let pyodide = null;
async function main() {
    while (true)
        try {
            pyodide = await loadPyodide();
            break;
        } catch {
            await sleep(1000);
        }
}
main();