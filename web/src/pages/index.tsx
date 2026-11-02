import dynamic from 'next/dynamic';
import Head from 'next/head';
const Application = dynamic(() => import('@/app/Application'), {
  ssr: false,
  loading: () => <div className="loading-screen">Carregando Zelo…</div>,
});
export default function Entry() {
  return (
    <>
      <Head>
        <title>Zelo — seu financeiro em boas mãos</title>
        <meta name="description" content="Entre no Zelo e cuide das finanças da sua empresa." />
        <meta name="referrer" content="no-referrer" />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
      </Head>
      <Application />
    </>
  );
}
