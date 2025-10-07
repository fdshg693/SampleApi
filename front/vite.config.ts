import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';

export default defineConfig({
	plugins: [sveltekit()],
	server: {
		// Dev proxy so the Svelte app can call the .NET API at http://localhost:5073 without CORS hassles
		proxy: {
			'/api': {
				// Backend redirects HTTP to HTTPS; target HTTPS directly to avoid 307s
				target: 'https://localhost:7082',
				changeOrigin: true,
				secure: false
			}
		}
	}
});
