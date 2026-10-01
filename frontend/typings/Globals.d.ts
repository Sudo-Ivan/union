declare module '*.module.css';

interface UnionInstanceConfig {
  apiKey: string;
  apiRoot: string;
  instanceName: string;
  theme: string;
  urlBase: string;
  version: string;
  isProduction: boolean;
  release?: string;
  branch?: string;
}

interface Window {
  Sonarr: UnionInstanceConfig;
  Radarr: UnionInstanceConfig;
  Union: UnionInstanceConfig;
}
