const fs = require('fs');
const path = require('path');

// Obtém o ambiente alvo (--env=production ou --env=development)
const args = process.argv.slice(2);
const envArg = args.find(arg => arg.startsWith('--env='));
const targetEnv = envArg ? envArg.split('=')[1] : (process.env.NODE_ENV || 'development');

const rootDir = path.resolve(__dirname, '..');
const envFilePath = path.join(rootDir, `.env.${targetEnv}`);
const fallbackEnvFilePath = path.join(rootDir, '.env');

let envFileToRead = fs.existsSync(envFilePath) ? envFilePath : fallbackEnvFilePath;

console.log(`[set-env] Configurando ambiente: "${targetEnv}" usando arquivo: ${envFileToRead}`);

let envVars = {};
if (fs.existsSync(envFileToRead)) {
  const content = fs.readFileSync(envFileToRead, 'utf-8');
  content.split('\n').forEach(line => {
    const trimmed = line.trim();
    if (trimmed && !trimmed.startsWith('#')) {
      const idx = trimmed.indexOf('=');
      if (idx > -1) {
        const key = trimmed.substring(0, idx).trim();
        const value = trimmed.substring(idx + 1).trim().replace(/^['"]|['"]$/g, '');
        envVars[key] = value;
      }
    }
  });
}

const isProd = targetEnv === 'production';
const apiUrl = envVars.API_URL || (isProd ? 'http://localhost:5050/api' : 'http://localhost:5050/api');

const envConfigFile = `// Arquivo gerado automaticamente pelo script set-env.js
export const environment = {
  production: ${isProd},
  apiUrl: (typeof window !== 'undefined' && (window as any).env?.API_URL)
    ? (window as any).env.API_URL
    : '${apiUrl}'
};
`;

const envDir = path.join(rootDir, 'src', 'environments');
if (!fs.existsSync(envDir)) {
  fs.mkdirSync(envDir, { recursive: true });
}

// Escreve no arquivo específico do ambiente (ex: environment.production.ts ou environment.development.ts)
const targetEnvFile = path.join(envDir, `environment.${targetEnv}.ts`);
fs.writeFileSync(targetEnvFile, envConfigFile, { encoding: 'utf-8' });
console.log(`[set-env] Gerado: ${targetEnvFile}`);

// Escreve também no environment.ts padrão
const defaultEnvFile = path.join(envDir, 'environment.ts');
fs.writeFileSync(defaultEnvFile, envConfigFile, { encoding: 'utf-8' });
console.log(`[set-env] Gerado: ${defaultEnvFile}`);
