import { defineConfig } from 'orval';

export default defineConfig({
  sampleapi: {
    input: {
      target: 'https://localhost:7082/openapi/v1.json',
      validation: false,
    },
    output: {
      mode: 'tags-split',
      target: 'src/lib/generated/api.ts',
      schemas: 'src/lib/generated/models',
      client: 'fetch',
      baseUrl: 'http://localhost:5073',
      override: {
        mutator: {
          path: 'src/lib/generated/custom-instance.ts',
          name: 'customInstance',
        },
      },
    },
  },
});
