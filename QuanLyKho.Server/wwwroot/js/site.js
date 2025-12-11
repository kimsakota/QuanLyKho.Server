// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener('keydown', e => {
    if (e.key.toLowerCase() === 'h') {
        const bar = document.getElementById('controlBar');
        bar.style.display = bar.style.display === 'none' ? 'flex' : 'none';
    }
});
