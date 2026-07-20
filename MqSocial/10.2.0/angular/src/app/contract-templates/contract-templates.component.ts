import { ChangeDetectorRef, Component, Injector, ViewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { finalize } from 'rxjs/operators';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppConsts } from '@shared/AppConsts';
import { PagedListingComponentBase } from 'shared/paged-listing-component-base';
import {
    ContractTemplateServiceProxy,
    ContractTemplateDto,
    ContractTemplateDtoPagedResultDto,
} from '@shared/service-proxies/service-proxies';
import { CreateContractTemplateDialogComponent } from './create-contract-template/create-contract-template-dialog.component';
import { EditContractTemplateDialogComponent } from './edit-contract-template/edit-contract-template-dialog.component';
import { buildDownloadFileName } from './contract-template-file.util';
import { Table, TableModule } from 'primeng/table';
import { LazyLoadEvent, PrimeTemplate } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { FormsModule } from '@angular/forms';
import { LocalizePipe } from '@shared/pipes/localize.pipe';
import { Button } from 'primeng/button';
import { Toolbar } from 'primeng/toolbar';
import { IconField } from 'primeng/iconfield';
import { InputIcon } from 'primeng/inputicon';
import { InputText } from 'primeng/inputtext';
import { Tooltip } from 'primeng/tooltip';

@Component({
    templateUrl: './contract-templates.component.html',
    animations: [appModuleAnimation()],
    standalone: true,
    imports: [
        FormsModule,
        TableModule,
        PrimeTemplate,
        PaginatorModule,
        LocalizePipe,
        Button,
        Toolbar,
        IconField,
        InputIcon,
        InputText,
        Tooltip,
    ],
})
export class ContractTemplatesComponent extends PagedListingComponentBase<ContractTemplateDto> {
    @ViewChild('dataTable', { static: true }) dataTable: Table;
    @ViewChild('paginator', { static: true }) paginator: Paginator;

    keyword = '';

    constructor(
        injector: Injector,
        private _contractTemplateService: ContractTemplateServiceProxy,
        private _modalService: BsModalService,
        private _http: HttpClient,
        cd: ChangeDetectorRef
    ) {
        super(injector, cd);
    }

    downloadFile(record: ContractTemplateDto): void {
        if (!record.filePath) return;

        const url = `${AppConsts.remoteServiceBaseUrl}/api/ContractTemplateFiles/${record.filePath}`;
        this._http.get(url, { responseType: 'blob' }).subscribe((blob) => {
            const objectUrl = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = objectUrl;
            a.download = buildDownloadFileName(record.name, record.filePath);
            a.click();
            window.URL.revokeObjectURL(objectUrl);
        });
    }

    createContractTemplate(): void {
        const modalRef: BsModalRef = this._modalService.show(CreateContractTemplateDialogComponent, { class: 'modal-lg' });
        modalRef.content.onSave.subscribe(() => this.refresh());
    }

    editContractTemplate(contractTemplate: ContractTemplateDto): void {
        const modalRef: BsModalRef = this._modalService.show(EditContractTemplateDialogComponent, {
            class: 'modal-lg',
            initialState: { id: contractTemplate.id },
        });
        modalRef.content.onSave.subscribe(() => this.refresh());
    }

    list(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator.changePage(0);
            if (this.primengTableHelper.records && this.primengTableHelper.records.length > 0) {
                return;
            }
        }

        this.primengTableHelper.showLoadingIndicator();

        this._contractTemplateService
            .getAll(
                this.keyword,
                this.primengTableHelper.getSorting(this.dataTable),
                this.primengTableHelper.getSkipCount(this.paginator, event),
                this.primengTableHelper.getMaxResultCount(this.paginator, event)
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result: ContractTemplateDtoPagedResultDto) => {
                this.primengTableHelper.records = result.items;
                this.primengTableHelper.totalRecordsCount = result.totalCount;
                this.primengTableHelper.hideLoadingIndicator();
                this.cd.detectChanges();
            });
    }

    delete(contractTemplate: ContractTemplateDto): void {
        abp.message.confirm(
            `Xóa mẫu hợp đồng "${contractTemplate.name}"?`,
            undefined,
            (result: boolean) => {
                if (result) {
                    this._contractTemplateService.delete(contractTemplate.id).subscribe(() => {
                        abp.notify.success(this.l('SuccessfullyDeleted'));
                        this.refresh();
                    });
                }
            }
        );
    }
}
